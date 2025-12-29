using Microsoft.Data.SqlClient;
using System.Data;

namespace QcChapWai.Data
{
    public class SqlHelper
    {
        private readonly string _connectionString;

        public SqlHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ??
                throw new ArgumentNullException("ConnectionString not found");
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<DataTable> ExecuteQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();

            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();
            adapter.Fill(dataTable);

            return dataTable;
        }

        public async Task<object?> ExecuteScalarAsync(string sql, params SqlParameter[] parameters)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            return await command.ExecuteScalarAsync();
        }

        public async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }

        public async Task<T?> ExecuteReaderAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
        {
            using var connection = GetConnection();
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
                return mapper(reader);

            return default;
        }

        public async Task<List<T>> ExecuteReaderListAsync<T>(string sql, Func<SqlDataReader, T> mapper, params SqlParameter[] parameters)
        {
            var results = new List<T>();

            using var connection = GetConnection();
            using var command = new SqlCommand(sql, connection);

            if (parameters != null)
                command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                results.Add(mapper(reader));
            }

            return results;
        }
    }
}