using Microsoft.Data.SqlClient;
using QcChapWai.Data;
using QcChapWai.Models;
using System.Data;

namespace QcChapWai.Services
{
    public class UserService
    {
        private readonly SqlHelper _sqlHelper;
        private readonly ILogger<UserService> _logger;

        public UserService(SqlHelper sqlHelper, ILogger<UserService> logger)
        {
            _sqlHelper = sqlHelper;
            _logger = logger;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            try
            {
                var sql = @"
                    SELECT [Id]
                          ,[Username]
                          ,[PasswordHash]
                          ,[Role]
                          ,[CreatedDate]
                          ,[IsActive]
                          ,[UserFullName]
                          ,[UserHead]
                          ,[CanEdit]
                          ,[UserTeam]
                    FROM Users 
                    WHERE Username = @username AND IsActive = 1";

                var user = await _sqlHelper.ExecuteReaderAsync(sql, MapUser, new SqlParameter("@username", username));

                // ✅ ตรวจสอบ user ไม่เป็น null ก่อน
                if (user != null)
                {
                    bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                    if (passwordMatch)
                    {
                        return user;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                // ✅ เพิ่ม logger injection ใน constructor หรือลบบรรทัดนี้ออก
                // _logger.LogError(ex, "Error validating user credentials for {Username}", username);
                Console.WriteLine($"Error validating user: {ex.Message}");
                return null; // ✅ return null แทน throw เพื่อไม่ให้ app crash
            }
        }

        public async Task<List<UserDropdownViewModel>> GetInspectorsTeamAAsync()
        {
            return await _sqlHelper.ExecuteReaderListAsync(
                @"SELECT Id, UserFullName, UserTeam
                  FROM Users
                  WHERE UserHead = 0 
                    AND UserTeam = 'A' 
                    AND IsActive = 1
                  ORDER BY UserFullName",
                MapUserDropdown
            );
        }

        // ============================================
        // ✅ ดึงผู้ตรวจสอบ กะ B
        // ============================================
        public async Task<List<UserDropdownViewModel>> GetInspectorsTeamBAsync()
        {
            return await _sqlHelper.ExecuteReaderListAsync(
                @"SELECT Id, UserFullName, UserTeam
                  FROM Users
                  WHERE UserHead = 0 
                    AND UserTeam = 'B' 
                    AND IsActive = 1
                  ORDER BY UserFullName",
                MapUserDropdown
            );
        }

        // ============================================
        // ✅ ดึงผู้ตรวจสอบทั้งหมด (A + B)
        // ============================================
        public async Task<List<UserDropdownViewModel>> GetAllInspectorsAsync()
        {
            return await _sqlHelper.ExecuteReaderListAsync(
                @"SELECT Id, UserFullName, UserTeam
                  FROM Users
                  WHERE UserHead = 0 
                    AND UserTeam IN ('A', 'B')
                    AND IsActive = 1
                  ORDER BY UserTeam, UserFullName",
                MapUserDropdown
            );
        }

        // ============================================
        // ✅ ดึงผู้อนุมัติ (UserHead = 1)
        // ============================================
        public async Task<List<UserDropdownViewModel>> GetApproversAsync()
        {
            return await _sqlHelper.ExecuteReaderListAsync(
                @"SELECT Id, UserFullName, UserTeam
                  FROM Users
                  WHERE UserHead = 1 
                    AND IsActive = 1
                  ORDER BY UserFullName",
                MapUserDropdown
            );
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var sql = @"
                SELECT [Id] ,[Username] ,[PasswordHash] ,[Role] ,[CreatedDate] ,[IsActive] ,[UserFullName] ,[UserHead] ,[CanEdit] ,[UserTeam] 
                FROM Users 
                WHERE Id = @id";

            return await _sqlHelper.ExecuteReaderAsync(sql, MapUser, new SqlParameter("@id", id));
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var sql = @"
                SELECT [Id] ,[Username] ,[PasswordHash] ,[Role] ,[CreatedDate] ,[IsActive] ,[UserFullName] ,[UserHead] ,[CanEdit] ,[UserTeam] 
                FROM Users 
                WHERE Username = @username";

            return await _sqlHelper.ExecuteReaderAsync(sql, MapUser, new SqlParameter("@username", username));
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var sql = @"
                SELECT [Id] ,[Username] ,[PasswordHash] ,[Role] ,[CreatedDate] ,[IsActive] ,[UserFullName] ,[UserHead] ,[CanEdit] ,[UserTeam] 
                FROM Users 
                ORDER BY Username";

            return await _sqlHelper.ExecuteReaderListAsync(sql, MapUser);
        }

        public async Task<int> CreateUserAsync(User user)
        {
            var sql = @"
                INSERT INTO Users ([Id] ,[Username] ,[PasswordHash] ,[Role] ,[CreatedDate] ,[IsActive] ,[UserFullName] ,[UserHead] ,[CanEdit] ,[UserTeam]
                VALUES (@Id ,@Username ,@PasswordHash ,@Role ,@CreatedDate ,@IsActive ,@UserFullName ,@UserHead ,@CanEdit ,@UserTeam);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            var parameters = new[]
            {
                new SqlParameter("@Id", user.Id),
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@PasswordHash", user.PasswordHash),
                new SqlParameter("@Role", user.Role),
                new SqlParameter("@CreatedDate", user.CreatedDate),
                new SqlParameter("@IsActive", user.IsActive),
                new SqlParameter("@UserFullName", user.UserFullName),
                new SqlParameter("@UserHead", user.UserHead),
                new SqlParameter("@CanEdit", user.CanEdit),
                new SqlParameter("@UserTeam", user.UserTeam)
            };

            var result = await _sqlHelper.ExecuteScalarAsync(sql, parameters);
            return Convert.ToInt32(result);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            var sql = @"
                UPDATE Users 
                SET Username = @Username, Role = @Role, IsActive = @IsActive
                WHERE Id = @Id";

            var parameters = new[]
            {
                new SqlParameter("@Id", user.Id),
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@PasswordHash", user.PasswordHash),
                new SqlParameter("@Role", user.Role),
                new SqlParameter("@CreatedDate", user.CreatedDate),
                new SqlParameter("@IsActive", user.IsActive),
                new SqlParameter("@UserFullName", user.UserFullName),
                new SqlParameter("@UserHead", user.UserHead),
                new SqlParameter("@CanEdit", user.CanEdit),
                new SqlParameter("@UserTeam", user.UserTeam)
            };

            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

            var sql = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Id = @Id";

            var parameters = new[]
            {
                new SqlParameter("@Id", userId),
                new SqlParameter("@PasswordHash", hashedPassword)
            };

            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, parameters);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var sql = "DELETE FROM Users WHERE Id = @id";
            var rowsAffected = await _sqlHelper.ExecuteNonQueryAsync(sql, new SqlParameter("@id", id));
            return rowsAffected > 0;
        }

        public async Task<bool> UserExistsAsync(string username, string email, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM Users WHERE (Username = @username OR Email = @email)";
            var parameters = new List<SqlParameter>
            {
                new("@username", username),
                new("@email", email)
            };

            if (excludeId.HasValue)
            {
                sql += " AND Id != @excludeId";
                parameters.Add(new SqlParameter("@excludeId", excludeId.Value));
            }

            var count = await _sqlHelper.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(count) > 0;
        }
        private UserDropdownViewModel MapUserDropdown(SqlDataReader reader)
        {
            return new UserDropdownViewModel
            {
                Id = reader.GetInt32("Id"),
                UserFullName = reader.GetString("UserFullName"),
                UserTeam = reader.IsDBNull("UserTeam") ? "" : reader.GetString("UserTeam")
            };
        }
        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                PasswordHash = reader.GetString("PasswordHash"),
                Role = reader.GetString("Role"),
                CreatedDate = reader.GetDateTime("CreatedDate"),
                IsActive = reader.GetBoolean("IsActive"),
                UserFullName = reader.GetString("UserFullName"),
                UserHead = reader.GetBoolean("UserHead"),
                CanEdit = reader.GetBoolean("CanEdit"),
                UserTeam = reader.GetString("UserTeam"),
            };
        }
    }
}