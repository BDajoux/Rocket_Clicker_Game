using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GameServerApi.Models
{
    public enum Role
    {
        AdminRole,
        UserRole
    }

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Username { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
        public Role Role { get; set; }

        public User() { }

        [SetsRequiredMembers]
        public User(UserPass userInfo)
        {
            Username = userInfo.Username;
            Password = userInfo.Password;
            Role = Role.UserRole;
        }

        public void SetPasswordHash(string hashedPassword)
        {
            Password = hashedPassword;
        }

        public bool VerifyPassword(string password, PasswordHasher<User> passwordHasher)
        {
            var result = passwordHasher.VerifyHashedPassword(this, this.Password, password);
            return result == PasswordVerificationResult.Success;
        }
    }

    public record UserPass
    {
        [Required(ErrorMessage = "Le pseudo est requis.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Le pseudo doit contenir entre 3 et 20 caractères.")]
        [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Le pseudo doit contenir uniquement des caractères alphanumériques.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Le mot de passe doit contenir entre 4 et 20 caractères.")]
        [RegularExpression(@"^[a-zA-Z0-9&^!@#]{4,20}$", ErrorMessage = "Le mot de passe doit contenir uniquement des lettres, chiffres et les caractères spéciaux &^!@#.")]
        public string Password { get; set; } = null!;

        [JsonConstructor]
        public UserPass() { }
    }

    public record UserPublic
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Role Role { get; set; }

        protected UserPublic(int id, string pseudo, Role role)
        {
            Id = id;
            Username = pseudo;
            Role = role;
        }

        public UserPublic(User user)
        {
            Id = user.Id;
            Username = user.Username;
            Role = user.Role;
        }
    }

    

    public record UserUpdate : UserPass
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Le mot de passe est requis.")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Le mot de passe doit contenir entre 4 et 20 caractères.")]
        [RegularExpression(@"^[a-zA-Z0-9&^!@#]{4,20}$", ErrorMessage = "Le mot de passe doit contenir uniquement des lettres, chiffres et les caractères spéciaux &^!@#.")]
        public new string Password { get; set; } = null!;
        public Role Role { get; set; }
    }
    
}
