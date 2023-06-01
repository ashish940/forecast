using System;
using System.Collections.Generic;
using System.Linq;
using Forecast.Data;
using System.IO;
using System.Data;
using Forecast.Models;
using System.IO.Compression;
using System.Dynamic;
using System.Threading.Tasks;
using System.Reflection;

namespace Forecast.Exports
{
    public class Exports
    {
        #region EXPORTS

        public string RunExport(DTParameterModel param, String dirPath)
        {
            string exportResults = string.Empty;

            switch(param.ExportChoice)
            {
                case "ItemPatchWeekTemplate":
                case "ItemMMWeekTemplate":
                case "ItemPatchTotalTemplate":
                case "ItemMMTotalTemplate":
                case "ItemRegionMMTemplate":
                    exportResults = RunExportTemplate(param, dirPath);
                    break;
                case "NewItemsUploadTemplate":
                case "ItemPatchOwnershipTemplate":
                case "LowesForecastingTemplate":
                    exportResults = RunExportTemplateFile(param, dirPath);
                    break;
                case "ItemPatchOverlapData":
                case "ItemPatchOwnershipData":
                    exportResults = RunExportReportNoTempTable(param, dirPath);
                    break;
                default:
                    exportResults = RunExportReport(param, dirPath);
                    break;
            }

            return exportResults;
        }

        /// <summary>
        /// Runs and creates an export with data for the an export according to the ExportChoice from 
        /// the <see cref="DTParameterModel"/> <paramref name="param"/> provided.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="exportChoice"> A <seealso cref="string"/> name for an export that 
        /// you wish to run. </param>
        /// <returns> A file path to an export crated. </returns>
        private string RunExportReport(DTParameterModel param, string dirPath)
        {
            var dp = new DataProvider();
            var UUID = Guid.NewGuid();
            var uuidStr = $"{UUID}".Replace("-", "").Substring(0, 10);
            var tableName = $"temp_{param.TableName}_{param.Username}_{UUID}".Replace("-", "");
            var listOfFilePaths = new List<string>();

            //Lets get the information needed for this report
            var exportInfo = GetExportInfo(param);

            // We need to call this to create a temp table in the database for performance.
            // This table is not an actual temp table but just a table with a very unique name.
            // We can't utilize a temp table because we need a column that autoincrements and temp table don't
            // support autoincrementing columns (IDENTITY columns).
            dp.CreateTableWithData(param, exportInfo, tableName);

            // Get total number of rows from the temp table
            var totalRowCount = dp.GetTempTableCount(tableName);

            //The number of files we'll have to make for this dataset
            var numberOfFiles = (int) Math.Ceiling(((totalRowCount / MaxRowsPerFile) * 1.0) + 0.5); //Need to always round up to next whole number

            // Offset keeps track of where to start the data pull in the temp table
            var offset = 0;

            // Get the first DataSet
            var dataSet = dp.GetExportReportWithData(param, exportInfo, tableName, offset);

            var numberOfRows = dataSet.Count();

            // Reset the offset for the Task
            offset = numberOfRows;

            try
            {
                for (var currentFile = 1; currentFile <= numberOfFiles; currentFile++)
                {
                    numberOfRows = dataSet.Count();

                    // This part clears the nextDataSet list for the GC and starts a task to 
                    // get the next dataset. This way we speed up the process a little.
                    IEnumerable<ExpandoObject> nextDataSet = new List<ExpandoObject>();
                    Task task = Task.Factory.StartNew(() =>
                    {
                        // We only want to start another task if the items returned are exactly 100000 rows
                        // if it's any less then it means it was the last set of data
                        if (numberOfRows == MaxRowsPerFile)
                        {
                            nextDataSet = dp.GetExportReportWithData(param, exportInfo, tableName, offset);
                        }
                    });

                    //Create a name for the file with an index after the file name prefix starting from 1 because users don't count from 0
                    var fileName = $"{Path.GetFileNameWithoutExtension(exportInfo.FileName)}_{currentFile}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{param.GMSVenID}{uuidStr}.csv";

                    // Create the next Excel file
                    var filePath = CreateFilePathFromExpandoList(dataSet, dirPath, fileName);
                    listOfFilePaths.Add(filePath);

                    // Record the next index to offset the data pull from 
                    offset += dataSet.Count();

                    // Wait for task to complete if it hasn't yet
                    Task.WaitAll(task);

                    dataSet = nextDataSet;
                }
            }
            catch (Exception e)
            {
                // Normally we would not have a try catch here but we need to make sure the 
                // temp table is dropped if an error arises.
                dp.DropTempExportTable(tableName);
                throw e;
            }

            //We need these to create the zip files the zip file
            var zipFileName = $"{Path.GetFileNameWithoutExtension(listOfFilePaths.Last())}.zip";
            var zipFilePath = Path.Combine(dirPath, zipFileName);

            // Zip up all the Excle files
            CreateZipFileFromFileList(zipFilePath, listOfFilePaths);

            // Clean up all Excel files
            DeleteFilesFromPathList(listOfFilePaths);

            // Drop the table we created in the database for this specific export
            dp.DropTempExportTable(tableName);

            return zipFileName;
        }

        /// <summary>
        /// Runs and creates an export with data for the an export according to the ExportChoice from 
        /// the <see cref="DTParameterModel"/> <paramref name="param"/> provided.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="exportChoice"> A <seealso cref="string"/> name for an export that 
        /// you wish to run. </param>
        /// <returns> A file path to an export crated. </returns>
        private string RunExportReportNoTempTable(DTParameterModel param, string dirPath)
        {
            var dp = new DataProvider();
            var UUID = Guid.NewGuid();
            var uuidStr = $"{UUID}".Replace("-", "").Substring(0, 10);
            var tableName = $"temp_{param.TableName}_{param.Username}_{UUID}".Replace("-", "");
            var listOfFilePaths = new List<string>();
           
            //Lets get the information needed for this report
            var exportInfo = GetExportInfo(param);

            // Get the first DataSet
            var dataSet = exportInfo.GetData(param, exportInfo);

            try
            {
                //Create a name for the file with an index after the file name prefix starting from 1 because users don't count from 0
                var fileName = $"{Path.GetFileNameWithoutExtension(exportInfo.FileName)}_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{param.GMSVenID}{uuidStr}.csv";

                // Create the next Excel file
                var filePath = CreateFilePathFromExpandoList(dataSet, dirPath, fileName);
                listOfFilePaths.Add(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }

            //We need these to create the zip files the zip file
            var zipFileName = $"{Path.GetFileNameWithoutExtension(listOfFilePaths.Last())}.zip";
            var zipFilePath = Path.Combine(dirPath, zipFileName);

            // Zip up all the Excle files
            CreateZipFileFromFileList(zipFilePath, listOfFilePaths);

            // Clean up all Excel files
            DeleteFilesFromPathList(listOfFilePaths);

            // Drop the table we created in the database for this specific export
            dp.DropTempExportTable(tableName);

            return zipFileName;
        }

        #endregion EXPORTS

        #region HELPERS

        public string CreateCsvExport<T>(List<T> dataList, string filePath)
        {
            var listOfRows = new List<DataRow>();

            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    var dtHeader = new DTHeaderNames();
                    var columns = dataList[0].GetType().GetProperties().Select(pi => 
                    {
                        var userFriendlyHeader = dtHeader.GetDTHeaderName(pi.Name);
                        return userFriendlyHeader ?? pi.Name;
                    });

                    sw.WriteLine(string.Join(",", columns));

                    foreach (var item in dataList)
                    {
                        var props = item.GetType().GetProperties().ToArray();
                        foreach (PropertyInfo prop in props)
                        {
                            var value = prop.GetValue(item);
                            if (value == null)
                            {
                                sw.Write($"{string.Empty},");
                            }
                            else if (value is DBNull)
                            {
                                sw.Write($"{value},");
                            }
                            else if (value.GetType() == typeof(decimal))
                            {
                                sw.Write($"{value},");
                            }
                            else if (Int32.TryParse(Convert.ToString(value), out int val))
                            {
                                sw.Write($"{val},");
                            }
                            else
                            {
                                sw.Write($"\"{value}\",");
                            }
                        }

                        sw.Write("\n");
                    }

                    sw.Flush();
                }
            }
            catch (Exception e)
            {
                var source = $"There was an error writing the export file to file path: {filePath}";
                throw e;
            }

            return filePath;
        }

        /// <summary>
        /// Creates a list of file paths from a <seealso cref="List{ExpandoObject}"/><paramref name="filePath"/>.
        /// </summary>
        /// <param name="expandoList">the <seealso cref="List{ExpandoObject}"/> that contains the data to write
        /// to the list of files.</param>
        /// <param name="filePath">The full path of the file.</param>
        public string CreateCsvExport(List<ExpandoObject> expandoList, string filePath)
        {
            var tableRows = expandoList.ToList();
            var listOfRows = new List<DataRow>();
            var expandoObj = expandoList.First();

            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    var dtHeader = new DTHeaderNames();
                    sw.WriteLine(string.Join(",", ((IDictionary<string, object>)expandoObj).Keys.Select(key => dtHeader.GetDTHeaderName(key))));

                    //Here we get the next 100000 rows from the data table into a new file
                    //We also make sure that we don't go past the row count in the datatable
                    for (var i = 0; i < expandoList.Count(); i++)
                    {
                        var expando = tableRows[i];
                        foreach (var colVal in expando)
                        {
                            sw.Write($"\"{colVal.Value}\",");
                        }

                        sw.Write("\n");
                    }

                    sw.Flush();
                }
            }
            catch (Exception e)
            {
                var source = $"There was an error writing the export file to file path: {filePath}";
                throw e;
            }

            return filePath;
        }

        /// <summary>
        /// Creates a list of file paths from a <seealso cref="IEnumerable{ExpandoObject}"/><paramref name="filePath"/>.
        /// </summary>
        /// <param name="expandoList">the <seealso cref="IEnumerable{ExpandoObject}"/> that contains the data to write
        /// to the list of files.</param>
        /// <param name="dirPath">The directory path of the file.</param>
        /// <param name="exportInfo.FileName">The name you want the files to have before their index and date.</param>
        public string CreateFilePathFromExpandoList(IEnumerable<ExpandoObject> expandoList, string dirPath, string fileName)
        {
            var filePath = dirPath + fileName;

            var tableRows = expandoList.ToList();
            var listOfRows = new List<DataRow>();

            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    var dtHeader = new DTHeaderNames();
                    sw.WriteLine(string.Join(",", expandoList.FirstOrDefault().Select(expando => dtHeader.GetDTHeaderName(expando.Key))));

                    //Here we get the next 100000 rows from the data table into a new file
                    //We also make sure that we don't go past the row count in the datatable
                    for (var i = 0; i < expandoList.Count(); i++)
                    {
                        var expando = tableRows[i];
                        foreach (var colVal in expando)
                        {
                            sw.Write($"{colVal.Value},");
                        }

                        sw.Write("\n");
                    }

                    sw.Flush();
                }
            }
            catch (Exception e)
            {
                var source = $"There was an error writing the export file to file path: {filePath}";
                throw e;
            }

            return filePath;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item MM Week" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemMMWeekInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(ItemMMWeekColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "ItemLGMWeek_data.csv",
                OrderByColumns = string.Join(",", ItemMMWeekColumns.Split(',').Take(3)),
                Select = dc.GetExportReportItemMMWeek(param)
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item MM Week" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemMMWeekTemplateInfo(DTParameterModel param)
        {
            var dc = new DataCommands();

            return new ExportInfo()
            {
                ColumnsToSelect = GetColumnsWithNulls(ItemMMWeekColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo()),
                FileName = "ItemLGMWeek_Template.csv"
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item MM Total" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemMMTotalInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(ItemMMTotalColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "ItemLGMTotal_data.csv",
                OrderByColumns = string.Join(",", ItemMMTotalColumns.Split(',').Take(2)),
                Select = dc.GetExportReportItemMMTotal(param)
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item MM Total" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemMMTotalTemplateInfo(DTParameterModel param)
        {
            var dc = new DataCommands();

            return new ExportInfo()
            {
                ColumnsToSelect = GetColumnsWithNulls(ItemMMTotalColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo()),
                FileName = "ItemLGMTotal_template.csv"
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Total" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchTotalInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(ItemPatchTotalColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "ItemPatchTotal_data.csv",
                OrderByColumns = string.Join(",", ItemPatchTotalColumns.Split(',').Take(2)),
                Select = dc.GetExportReportItemPatchTotal(param)
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Total" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchTotalTemplateInfo(DTParameterModel param)
        {
            var dc = new DataCommands();

            return new ExportInfo()
            {
                ColumnsToSelect = GetColumnsWithNulls(ItemPatchTotalColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo()),
                FileName = "ItemPatchTotal_template.csv"
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Week" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchWeekInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(ItemPatchWeekColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "ItemPatchWeek_data.csv",
                OrderByColumns = string.Join(",", ItemPatchWeekColumns.Split(',').Take(3)),
                Select = dc.GetExportReportItemPatchWeek(param)
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Week" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchWeekTemplateInfo(DTParameterModel param)
        {
            var dc = new DataCommands();

            return new ExportInfo()
            {
                ColumnsToSelect = GetColumnsWithNulls(ItemPatchWeekColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo()),
                FileName = "ItemPatchWeek_Template.csv"
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Region MM" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemRegionMMInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(ItemRegionMMColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "ItemRegionLGM_data.csv",
                OrderByColumns = string.Join(",", ItemRegionMMColumns.Split(',').Take(2)),
                Select = dc.GetExportReportItemRegionMM(param)
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Region MM" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemRegionMMTemplateInfo(DTParameterModel param)
        {
            var dc = new DataCommands();

            return new ExportInfo()
            {
                ColumnsToSelect = GetColumnsWithNulls(ItemRegionMMColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo()),
                FileName = "ItemRegionLGM_Template.csv"
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Lowe's Forecasting" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateLowesForecastingTemplateInfo(DTParameterModel param)
        {
            var exportTemplateInfo = new DataProvider().GetLowesForecastingFileInfo();

            if (exportTemplateInfo.FileName != null)
            {
                return new ExportInfo()
                {
                    FileName = exportTemplateInfo.FileName
                };
            }

            return null;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "0 Forecast Items" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateNewItemsInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(NewItemsColumns.Split(',').ToList(), dc.GetForecastTableColumnInfo());

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = string.Join(",", colsToSelect.Keys),
                FileName = "NewItems.csv",
                OrderByColumns = NewItemsColumns,
                Select = dc.GetExportReportNewItems(param),
            };
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "New Items Upload" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateNewItemsUploadTemplateInfo(DTParameterModel param)
        {
            var exportTemplateInfo = new DataProvider().GetNewItemUploadColumns();

            if (exportTemplateInfo.FileName != null)
            {
                return new ExportInfo()
                {
                    FileName = exportTemplateInfo.FileName
                };
            }

            return null;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Ownership" template. This 
        /// contains the columns names that the template needs and the file name the template will use.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchOwnershipTemplateInfo(DTParameterModel param)
        {
            var exportTemplateInfo = new DataProvider().GetItemPatchOwnershipColumns();

            if (exportTemplateInfo.FileName != null)
            {
                return new ExportInfo()
                {
                    FileName = exportTemplateInfo.FileName
                };
            }

            return null;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Ownership Data" export. This 
        /// contains the columns the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchOwnershipInfo(DTParameterModel param)
        {
            var info = new ExportInfo()
            {
                FileName = "ItemPatchOwnership_data.csv",
                GetData = new DataProvider().GetExportReportItemPatchOwnershipList
            };

            return info;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Item Patch Overlap Data" export. This 
        /// contains the columns the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateItemPatchOverlapInfo(DTParameterModel param)
        {
            var info = new ExportInfo()
            {
                FileName = "ItemPatchOverlap_data.csv",
                GetData = new DataProvider().GetExportReportOverlappingItemPatch
            };

            return info;
        }

        /// <summary>
        /// Creates an <seealso cref="ExportInfo"/> object for the "Export Filtered Data" export. This 
        /// contains the columns that the export needs to select, columns that it needs to build a temp table,
        /// the file name the export will use, any columns that the report should be ordered by and the 
        /// select statement that will be used to select the data into the temp table.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        private ExportInfo CreateFullDownloadExportInfo(DTParameterModel param)
        {
            var dc = new DataCommands();
            var colsToSelect = GetColumnsWithNulls(GetColumns(param), dc.GetForecastTableColumnInfo());

            var orderByCols = new List<string>();
            foreach(var col in RotatorColumns.Split(','))
            {
                if(colsToSelect.Keys.Contains(col, new CompareIgnoreCase()))
                {
                    orderByCols.Add(col);
                }
            }

            return new ExportInfo()
            {
                ColumnsToSelect = colsToSelect,
                ColumnsToBuild = dc.GetExportReportForecastColumns(),
                FileName = "ForecastDownload.csv",
                OrderByColumns = string.Join(",", orderByCols),
                Select = dc.GetExportReportForecastDownload(param)
            };
        }

        /// <summary>
        /// Gets a <seealso cref="ExportInfo"/> with the first item being the column names
        /// the second item is the file name needed for an export and the third item is the command needed
        /// to execute to get the data from the database. It can be a template or template with data.
        /// </summary>
        /// <param name="param"></param> <seealso cref="DTParameterModel"/> has a ExportChoice property 
        /// that is the export option that you wish to get the columns for.
        /// <returns>A <seealso cref="ExportInfo"/> with ColumnsToSelect being a <seealso cref="String"/> of column names 
        /// separated by a comma (,), ColumnsToBuild will be used to build a temp table,
        /// FileName is a <seealso cref="string"/> that is the name of the file, and Select is
        /// a <seealso cref="string"/> SQL command to get the data from the database.
        /// If the <paramref name="param"/> isn't found then an empty 
        /// <seealso cref="ExportInfo"/> will be returned.</returns>
        public ExportInfo GetExportInfo(DTParameterModel param)
        {
            switch (param.ExportChoice)
            {
                case "ItemPatchWeekTemplate":
                    return CreateItemPatchWeekTemplateInfo(param);
                case "ItemPatchWeekData":
                    return CreateItemPatchWeekInfo(param);
                case "ItemMMWeekTemplate":
                    return CreateItemMMWeekTemplateInfo(param);
                case "ItemMMWeekData":
                    return CreateItemMMWeekInfo(param);
                case "ItemPatchTotalTemplate":
                    return CreateItemPatchTotalTemplateInfo(param);
                case "ItemPatchTotalData":
                    return CreateItemPatchTotalInfo(param);
                case "ItemMMTotalTemplate":
                    return CreateItemMMTotalTemplateInfo(param);
                case "ItemMMTotalData":
                    return CreateItemMMTotalInfo(param);
                case "ItemRegionMMTemplate":
                    return CreateItemRegionMMTemplateInfo(param);
                case "ItemRegionMMData":
                    return CreateItemRegionMMInfo(param);
                case "NewItemsExport":
                    return CreateNewItemsInfo(param);
                case "NewItemsUploadTemplate":
                    return CreateNewItemsUploadTemplateInfo(param);
                case "ItemPatchOwnershipTemplate":
                    return CreateItemPatchOwnershipTemplateInfo(param);
                case "ItemPatchOwnershipData":
                    return CreateItemPatchOwnershipInfo(param);
                case "ItemPatchOverlapData":
                    return CreateItemPatchOverlapInfo(param);
                case "ExportReportFullDownload":
                    return CreateFullDownloadExportInfo(param);
                case "LowesForecastingTemplate":
                    return CreateLowesForecastingTemplateInfo(param);
                default:
                    return new ExportInfo();
            }
        }

        /// <summary>
        /// Column names for the Item MM Totals export
        /// </summary>
        private string ItemMMTotalColumns { get; } = "ItemID,MM,SalesUnits_FC,Vendor_Comments";

        /// <summary>
        /// Column names for the Item MM Week export
        /// </summary>
        private string ItemMMWeekColumns { get; }  = "ItemID,MM,FiscalWk,SalesUnits_FC";

        /// <summary>
        /// Column names for the Item Patch Total export
        /// </summary>
        private string ItemPatchTotalColumns { get; }  = "ItemID,Patch,Cost_LY,Cost_TY,Cost_FC,RetailPrice_LY,RetailPrice_TY,RetailPrice_FC,SalesUnits_FC";

        /// <summary>
        /// Column names for the Item Patch Week export
        /// </summary>
        private string ItemPatchWeekColumns { get; } = "ItemID,Patch,FiscalWk,Cost_LY,Cost_TY,Cost_FC,RetailPrice_LY,RetailPrice_TY,RetailPrice_FC,SalesUnits_FC";

        /// <summary>
        /// Column names for the Item Region MM export
        /// </summary>
        private string ItemRegionMMColumns { get; } = "ItemID,Region,MM,SalesUnits_FC";

        /// <summary>
        /// Column names for the New Items export
        /// </summary>
        private string NewItemsColumns { get; } = "ItemID,Patch,MM";

        /// <summary>
        /// Column names for the New Items Upload template
        /// </summary>
        private string NewItemsUploadColumns { get; } = "ItemID,ItemDesc,Patch,ProdGrpID,ParentID,AssrtId";

        /// <summary>
        /// Column names that appear in the Rotator dropdown list.
        /// </summary>
        private string RotatorColumns { get; } = "VendorDesc,ItemID,FiscalWk,FiscalMo,FiscalQtr,MD,MM,Region,District,Patch,CategoryID,ProdGrpID,AssrtID";

        /// <summary>
        /// Creates a zip file from a list of file paths. Works well with <seealso cref="CreateFilePathListFromDatatable(DataTable, string)"/>.
        /// </summary>
        /// <param name="zipFilePath">The path and the zip file name included.</param>
        /// <param name="fileList">A <seealso cref="List{string}"/> of file paths with the file names included.</param>
        public void CreateZipFileFromFileList(string zipFilePath, List<string> fileList)
        {
            //Create a copy of the list so we don't modify the original one
            var listOfPaths = fileList.ToList();
            try
            {
                //Just in case the same file exists, delete it
                if (File.Exists(Path.GetFullPath(zipFilePath)))
                {
                    File.Delete(Path.GetFullPath(zipFilePath));
                }

                //We create a file with the first file because we can't create an empty zip file
                using (var fileStream = new FileStream(Path.GetFullPath(zipFilePath), FileMode.CreateNew))
                {
                    using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                    {
                        zipArchive.CreateEntryFromFile(listOfPaths.First(), Path.GetFileName(listOfPaths.First()));
                    }
                }

                listOfPaths.RemoveAt(0);//Remove the first one because we already used it

                //If we still have more files then update the zip file with the remaining files. We can't 
                //do this step when creating a zip file because we have to specifically tell it that we are
                //updating the zip file.
                if (listOfPaths.Count > 0)
                {
                    using (var fileStream = new FileStream(Path.GetFullPath(zipFilePath), FileMode.Open))
                    {
                        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                        {
                            foreach (var file in listOfPaths)
                            {
                                zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var source = $"Couldn't create zip file from file list {listOfPaths.ToList()}";
                throw e;
            }
        }

        #endregion HELPERS

        #region TEMPLATES

        /// <summary>
        /// Exports various simple templates.  One row is easy to export.  Just add the necessary logic below and
        /// the proper checks in the RunExport() method.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string RunExportTemplate(DTParameterModel param, string dirPath)
        {
            // Get the column names and the file name for the template that was selected 
            // from param.ExportChoice
            var templateInfo = GetExportInfo(param);

            string csvFile = string.Empty;

            // Assign the column names here
            var dtHeader = new DTHeaderNames();
            string exportResults = string.Join(",", templateInfo.ColumnsToSelect.Keys.Select(key => dtHeader.GetDTHeaderName(key)));

            // Assign the file name here
            var fileName = templateInfo.FileName;
            var fileExtension = Path.GetExtension(fileName);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var uuid = Guid.NewGuid().ToString().Substring(0, 13);
            var uniqueFileName = $"{fileNameNoExt}_{uuid}{fileExtension}";

            string filePath = Path.Combine(dirPath, uniqueFileName);

            try
            {
                using (var sw = new StreamWriter(filePath))
                {
                    sw.WriteLine(exportResults);
                    sw.Flush();
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            GC.Collect();

            //return fileName instead of full filepath 
            return uniqueFileName;
        }

        /// <summary>
        /// Exports a pre-made file in the VerticaFTP\Forecast\Templates folder for the user.
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public string RunExportTemplateFile(DTParameterModel param, string dirPath)
        {
            // Get the file name from the back-end
            var templateInfo = GetExportInfo(param);

            // Set the local path to save the file to from the FTP location.
            var ftpTemplateFilePath = Path.Combine(Util.FTPVerticaForecastTemplatesPath, templateInfo.FileName);
            var fileExtension = Path.GetExtension(templateInfo.FileName);
            var fileNameNoExt = Path.GetFileNameWithoutExtension(templateInfo.FileName);
            var uuid = Guid.NewGuid().ToString().Substring(0, 13);
            var uniqueFileName = $"{fileNameNoExt}_{uuid}{fileExtension}";
            var localFilePath = Path.Combine(dirPath, uniqueFileName);

            try
            {
                // Copy the files.
                File.Copy(ftpTemplateFilePath, localFilePath);
            }
            catch (Exception e)
            {
                throw e;
            }

            return uniqueFileName;
        }

        #endregion TEMPLATES

        #region Utils

        /// <summary>
        /// Deletes a file if it exists in the given path.
        /// </summary>
        /// <param name="path"> A <seealso cref="string"/> that is a full that to the file 
        /// that you want to be deleted.</param>
        private void DeleteFileFromPath(string path)
        {
            if (File.Exists(Path.GetFullPath(path)))
            {
                File.Delete(Path.GetFullPath(path));
            }
        }

        /// <summary>
        /// Deletes all files from the list of file paths if they exist.
        /// </summary>
        /// <param name="paths"> An <seealso cref="IEnumerable{string}"/> of file paths to be deleted.</param>
        private void DeleteFilesFromPathList(IEnumerable<string> paths)
        {
            foreach (var file in paths)
            {
                DeleteFileFromPath(file);
            }
        }

        /// <summary>
        /// Gets a list of column names. The default is visible columns but you can search for invisible 
        /// columns by setting <paramref name="visible"/> to false.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="visible"></param>
        /// <returns></returns>
        public static List<string> GetColumns(DTParameterModel param, bool visible = true)
        {
            List<string> columns = new List<string>();
            
            foreach (DTColumn column in param.Columns)
            {
                if (column.Visible == visible)
                    columns.Add(column.Name);
            }

            return columns;
        }

        /// <summary>
        /// Builds a list of columns as keys and values as the datatypes.
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public IDictionary<string, string> GetColumnsWithNulls(List<string> columns, IDictionary<string, string> types)
        {
            var columnsWithTypes = new Dictionary<string, string>();

            foreach(var column in columns)
            {
                var tempType = types.FirstOrDefault(t => t.Key.Equals(column, StringComparison.CurrentCultureIgnoreCase));
                var sVal = tempType.Value.Substring(0, 3);
                var nullVal = (sVal.Equals("VAR") || sVal.Equals("NVA")) ? "" : "0";
                columnsWithTypes.TryGetValue(tempType.Key, out string typeValue);
                if (typeValue == null)
                    columnsWithTypes.Add(tempType.Key, nullVal);
                else
                    columnsWithTypes[tempType.Key] = nullVal;
            }

            return columnsWithTypes;
        }

        /// <summary>
        /// The maximum rows a file can have for and export
        /// </summary>
        public int MaxRowsPerFile { get; } = 100000;

        #endregion
    }
}