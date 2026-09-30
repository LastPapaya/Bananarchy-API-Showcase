using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BananarchyAPI.Data;
using BananarchyAPI.Dtos;
using BananarchyAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BananarchyAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BattleController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BattleController(AppDbContext context)
        {
            _context = context;
        }

        
        [HttpPost("attack")]
        public async Task<IActionResult> Attack([FromBody] AttackRequestDto request)
        {
            if (request == null) return BadRequest("Request is null");
            if (request.AttackerId <= 0 || request.DefenderId <= 0) return BadRequest("Invalid attacker/defender id");
            if (request.AttackerId == request.DefenderId) return BadRequest("You can't attack yourself");

            await using var tx = await _context.Database.BeginTransactionAsync();

            var attacker = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AttackerId);
            var defender = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.DefenderId);

            if (attacker == null) return NotFound("Attacker not found");
            if (defender == null) return NotFound("Defender not found");

            
            if (attacker.ArmyCount <= 0) return BadRequest("Attacker has no army");

            int aBefore = attacker.ArmyCount;
            int dBefore = defender.ArmyCount;

            int winnerId, loserId;
            int aAfter, dAfter;

            
            if (dBefore <= 0)
            {
                winnerId = attacker.Id;
                loserId = defender.Id;

                aAfter = aBefore;
                dAfter = 0;
            }
            else if (aBefore >= dBefore)
            {
                winnerId = attacker.Id;
                loserId = defender.Id;

                int loss = (int)Math.Ceiling(dBefore * 0.5);
                aAfter = Math.Max(0, aBefore - loss);
                dAfter = 0;
            }
            else
            {
                winnerId = defender.Id;
                loserId = attacker.Id;

                int loss = (int)Math.Ceiling(aBefore * 0.5);
                dAfter = Math.Max(0, dBefore - loss);
                aAfter = 0;
            }

            attacker.ArmyCount = aAfter;
            defender.ArmyCount = dAfter;

            
            int bananasStolen;
            if (winnerId == attacker.Id)
                bananasStolen = TransferBananas(defender, attacker, amount: 25);
            else
                bananasStolen = TransferBananas(attacker, defender, amount: 25);

            var log = new BattleLog
            {
                AttackerId = attacker.Id,
                DefenderId = defender.Id,
                WinnerId = winnerId,
                LoserId = loserId,

                AttackerArmyBefore = aBefore,
                DefenderArmyBefore = dBefore,
                AttackerArmyAfter = attacker.ArmyCount,
                DefenderArmyAfter = defender.ArmyCount,

                BananasStolen = bananasStolen,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.BattleLogs.Add(log);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            var result = new AttackResultDto
            {
                AttackerId = attacker.Id,
                DefenderId = defender.Id,
                WinnerId = winnerId,
                LoserId = loserId,
                AttackerArmyBefore = aBefore,
                DefenderArmyBefore = dBefore,
                AttackerArmyAfter = attacker.ArmyCount,
                DefenderArmyAfter = defender.ArmyCount,
                BananasStolen = bananasStolen
            };

            return Ok(result);
        }

        
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs([FromQuery] int? playerId = null, [FromQuery] int take = 50)
        {
            take = Math.Clamp(take, 1, 200);

            var q = _context.BattleLogs.AsNoTracking();

            if (playerId.HasValue && playerId.Value > 0)
            {
                int pid = playerId.Value;
                q = q.Where(l => l.AttackerId == pid || l.DefenderId == pid);
            }

            var logs = await q
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(take)
                .Select(l => new BattleLogDto
                {
                    CreatedAtUtc = l.CreatedAtUtc,

                    AttackerId = l.AttackerId,
                    AttackerUsername = _context.Users
                        .Where(u => u.Id == l.AttackerId)
                        .Select(u => u.Username)
                        .FirstOrDefault() ?? "",

                    DefenderId = l.DefenderId,
                    DefenderUsername = _context.Users
                        .Where(u => u.Id == l.DefenderId)
                        .Select(u => u.Username)
                        .FirstOrDefault() ?? "",

                    WinnerId = l.WinnerId,
                    LoserId = l.LoserId,

                    BananasStolen = l.BananasStolen,

                    AttackerArmyBefore = l.AttackerArmyBefore,
                    DefenderArmyBefore = l.DefenderArmyBefore,
                    AttackerArmyAfter = l.AttackerArmyAfter,
                    DefenderArmyAfter = l.DefenderArmyAfter
                })
                .ToListAsync();

            return Ok(logs);
        }

        

        private static int TransferBananas(User from, User to, int amount)
        {
            var fromRes = ReadResources(from.ResourcesJson);
            var toRes = ReadResources(to.ResourcesJson);

            int fromBananas = GetBananas(fromRes);
            int stolen = Math.Clamp(amount, 0, fromBananas);

            SetBananas(fromRes, fromBananas - stolen);
            SetBananas(toRes, GetBananas(toRes) + stolen);

            from.ResourcesJson = JsonSerializer.Serialize(fromRes);
            to.ResourcesJson = JsonSerializer.Serialize(toRes);

            return stolen;
        }

        private static Dictionary<string, int> ReadResources(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                return dict ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static int GetBananas(Dictionary<string, int> res)
        {
            if (res.TryGetValue("bananas", out int v)) return v;
            return 0;
        }

        private static void SetBananas(Dictionary<string, int> res, int value)
        {
            res["bananas"] = Math.Max(0, value);
        }
    }
}
