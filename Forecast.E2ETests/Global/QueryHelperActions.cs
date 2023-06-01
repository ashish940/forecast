namespace Forecast.E2ETests.Global
{
    class QueryHelperActions
    {

        /*
        public List<> QueryDatabase()
        {
            var result = new List<FilterParameter>();
            VerticaDataAdapter adapter = new VerticaDataAdapter();
            VerticaConnection connection = new VerticaConnection(_verticaWebConn);

            try
            {
                connection.Open();
                VerticaCommand command = connection.CreateCommand();
                command.CommandText = new DataCommands().GetFilterData(param, type, search);
                VerticaDataReader dr = command.ExecuteReader();
                int i = 1;
                while (dr.Read())
                {
                    result.Add(new FilterParameter { id = i, text = Convert.ToString(dr["Filter"]) });
                    i++;
                }
                dr.Close();
                connection.Close();
            }
            catch (Exception e)
            {

            }
            return result;
        }
        */
    }
}
