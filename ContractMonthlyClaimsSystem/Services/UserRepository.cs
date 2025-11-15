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

        public async Task<List<User>> GetUsersAsync()
        {
            return await Task.FromResult(_users);
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
        }

        public async Task<User> GetUserByCredentialsAsync(string username, string password)
        {
            var user = _users.FirstOrDefault(u =>
                (u.Name + " " + u.Surname).ToLower() == username.ToLower() &&
                u.Password == password);
            return await Task.FromResult(user);
        }

        public async Task AddUserAsync(User user)
        {
            user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
            _users.Add(user);
            await Task.CompletedTask;
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Surname = user.Surname;
                existingUser.Password = user.Password;
                existingUser.Role = user.Role;
            }
            await Task.CompletedTask;
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
            await Task.CompletedTask;
        }
    }

}
