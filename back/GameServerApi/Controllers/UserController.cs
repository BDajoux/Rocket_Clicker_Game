using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using GameServerApi.Hubs;
using Microsoft.AspNetCore.SignalR;


namespace GameServerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("user-limit")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IHubContext<ChatHub> _hubContext;
        

        public UserController(UserService userService, IHubContext<ChatHub> hubContext)
        {
            _userService = userService;
            _hubContext = hubContext;
            
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _userService.GetUser(id);
            return Ok(user);
        }

        // POST api/User/Register
        [HttpPost("Register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserPublic>> Register([FromBody] UserPass uInfo)
        {
            var result = await _userService.Register(uInfo);
            return Ok(result);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserPublic>> Login([FromBody] UserPass loginInfo)
        {
            var result = await _userService.Login(loginInfo);
            await _hubContext.Clients.All.SendAsync(
                "Login",
                result.User.Id
            );
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "AdminRole")]
        public async Task<ActionResult<IEnumerable<User>>> UpdateUser([FromBody] UserUpdate uUpdate)
        {
            var user = await _userService.UpdateUser(uUpdate);
            return Ok(user);
        }


        [HttpDelete("{id}")]
        [Authorize(Roles = "AdminRole")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.DeleteUser(id);
            return Ok("true");
        }

        [HttpGet("All")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserPublic>>> ReturnAllUsers()
        {
            var users = await _userService.GetAllUsers();
            return Ok(users);
        }

        [HttpGet("AllAdmin")]
        [Authorize(Roles = "AdminRole")]
        public async Task<ActionResult<IEnumerable<UserPublic>>> ReturnAllAdmin()
        {
            var users = await _userService.GetAllAdmin();
            return Ok(users);
        }

        [HttpGet("Search/{Name}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserPublic>>> ReturnMatchingNames(string Name)
        {
            var users = await _userService.SearchUsersByName(Name);
            return Ok(users);
        }
    }
}
