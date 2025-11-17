using ContractMonthlyClaimsSystem.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContractMonthlyClaimsSystem.Services
{
    public class UserRepository : IUserRepository
    {
        private static List<User> _users = new List<User>();
        public User CurrentUser { get; set; }

        public UserRepository()
        {
            // Add some sample users for testing
            if (!_users.Any())
            {
                _users.Add(new User
                {
                    Id = 1,
                    Name = "John",
                    Surname = "Doe",
                    Password = "password",
                    Role = "Lecturer"
                });
                _users.Add(new User
                {
                    Id = 2,
                    Name = "Jane",
                    Surname = "Smith",
                    Password = "password",
                    Role = "Manager"
                });
                _users.Add(new User
                {
                    Id = 3,
                    Name = "Bob",
                    Surname = "Johnson",
                    Password = "password",
                    Role = "Program Coordinator"
                });
            }
        }

        public Task<List<User>> GetUsersAsync()
        {
            return Task.FromResult(_users);
        }

        public Task<User> GetUserByIdAsync(int id)
        {
            return Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public Task<User> GetUserByCredentialsAsync(string username, string password)
        {
            // Split the username into name and surname
            var nameParts = username.Split(' ');
            if (nameParts.Length < 2)
            {
                return Task.FromResult<User>(null);
            }

            var name = nameParts[0];
            var surname = string.Join(" ", nameParts.Skip(1));

            var user = _users.FirstOrDefault(u =>
                u.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase) &&
                u.Surname.Equals(surname, System.StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            return Task.FromResult(user);
        }

        public Task<bool> UserExistsAsync(string name, string surname)
        {
            var exists = _users.Any(u =>
                u.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase) &&
                u.Surname.Equals(surname, System.StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(exists);
        }

        public async Task AddUserAsync(User user)
        {
            user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
            _users.Add(user);
            await Task.CompletedTask;
        }

        public Task UpdateUserAsync(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Surname = user.Surname;
                existingUser.Password = user.Password;
                existingUser.Role = user.Role;
            }
            return Task.CompletedTask;
        }

        public Task DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
            return Task.CompletedTask;
        }
    }

}
