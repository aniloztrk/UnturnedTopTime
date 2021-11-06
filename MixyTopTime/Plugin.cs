using Rocket.API;
using Rocket.Core.Commands;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;

namespace MixyTopTime
{
    public class Plugin : RocketPlugin<Config>
    {
        #region Commands
        [RocketCommand("toptime", "", "", AllowedCaller.Both)]
        [RocketCommandPermission("mixy.toptime")]
        public void ToptimeCommand(IRocketPlayer caller)
        {
            if (caller is UnturnedPlayer player)
            {
                foreach (var playerDb in Configuration.Instance.PlayerDataBase.OrderBy(t => t.PlayingTime).Reverse().Take(5))
                {
                    if (Provider.clients.FirstOrDefault(p => p.playerID.steamID.m_SteamID == playerDb.SteamId) != null)
                    {
                        var loopPlayer = UnturnedPlayer.FromCSteamID((CSteamID)playerDb.SteamId);
                        UnturnedChat.Say(player, $"{loopPlayer.DisplayName} - <color=white>{Math.Ceiling(playerDb.PlayingTime / 60)}</color> Min.", Color.yellow, true);
                    }
                    else
                    {
                        UnturnedChat.Say(player, $"{playerDb.DisplayName} - <color=white>{Math.Ceiling(playerDb.PlayingTime / 60)}</color> Min.", Color.red, true);
                    }
                    return;
                }
            }         
            else
            {
                foreach (var playerDb in Configuration.Instance.PlayerDataBase.OrderBy(t => t.PlayingTime).Reverse().Take(20))
                {
                    ConsoleSend(playerDb);
                    return;
                }
            }
            
        }
        [RocketCommand("playtime", "", "", AllowedCaller.Both)]
        [RocketCommandPermission("mixy.playtime")] 
        public void PlayTime(IRocketPlayer caller, string[] args)
        {
            if (caller is UnturnedPlayer player)
            {
                if (args.Length == 0)
                {
                    var playerDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == player.CSteamID.m_SteamID);
                    if (playerDb == null)
                    {
                        UnturnedChat.Say(player, "Your data not found.", Color.red);
                        return;
                    }
                    UnturnedChat.Say(player, $"<color=white>{Math.Ceiling(playerDb.PlayingTime / 60)}</color> Min.", Color.yellow, true);
                    return;
                }
                if (ulong.TryParse(args[0], out var result))
                {
                    var targetDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == result);
                    if (targetDb == null)
                    {
                        UnturnedChat.Say(player, "Player data not found.", Color.red);
                        return;
                    }
                    UnturnedChat.Say(player, $"<color=white>{Math.Ceiling(targetDb.PlayingTime / 60)}</color> Min.", Color.cyan, true);
                }
                else
                {
                    var target = UnturnedPlayer.FromName(args[0]);
                    var targetDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == target.CSteamID.m_SteamID);
                    if (targetDb == null)
                    {
                        UnturnedChat.Say(player, "Player data not found.", Color.red);
                        return;
                    }
                    UnturnedChat.Say(player, $"<color=white>{Math.Ceiling(targetDb.PlayingTime / 60)}</color> Min.", Color.cyan, true);
                }                
            }
            else
            {
                if (args.Length == 0)
                {
                    ConsoleWarn("Wrong usage. Usage : playtime <steamid>");
                    return;
                }
                if (ulong.TryParse(args[0], out var result))
                {
                    var targetDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == result);
                    if (targetDb == null)
                    {
                        ConsoleWarn("Player data not found.");
                        return;
                    }
                    ConsoleSend(targetDb);
                }
                else
                {
                    var targetDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.DisplayName.ToLower().StartsWith(args[0]) || p.DisplayName == args[0].ToLower() || p.DisplayName.ToLower().Contains(args[0]));
                    if (targetDb == null)
                    {
                        ConsoleWarn("Player data not found.");
                        return;
                    }
                    ConsoleSend(targetDb);
                }
            }
        }
        #endregion
        protected override void Load()
        {
            U.Events.OnPlayerConnected += Join;
            U.Events.OnPlayerDisconnected += Quit;

            StartCoroutine(TimeincreaseEversec());
        }
        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= Join;
            U.Events.OnPlayerDisconnected -= Quit;

            StopCoroutine(TimeincreaseEversec());
        }
        private void Join(UnturnedPlayer player)
        {
            var playerDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == player.CSteamID.m_SteamID);
            if (playerDb != null)
            {
                playerDb.DisplayName = player.DisplayName;
                Configuration.Save();
                return;
            }
            Configuration.Instance.PlayerDataBase.Add(new PlayerDb { SteamId = player.CSteamID.m_SteamID, DisplayName = player.DisplayName, PlayingTime = 0 }); 
            Configuration.Save();          
        }
        private void Quit(UnturnedPlayer player)
        {
            var playerDb = Configuration.Instance.PlayerDataBase.FirstOrDefault(p => p.SteamId == player.CSteamID.m_SteamID);
            if (playerDb != null)
            {
                playerDb.DisplayName = player.DisplayName;
                Configuration.Save();
                return;
            }
        }
        private IEnumerator TimeincreaseEversec()
        {
            while (true)
            {
                Timeincreaser();
                yield return new WaitForSeconds(1);
            }
        }
        private async void Timeincreaser()
        {
            await RawTimeincreaser();
        }
        private Task RawTimeincreaser()
        {
            return Task.Run(() =>
            {
                foreach (var playerDb in Configuration.Instance.PlayerDataBase)
                {
                    if (Provider.clients.FirstOrDefault(p => p.playerID.steamID.m_SteamID == playerDb.SteamId) != null)
                    {
                        playerDb.PlayingTime++;
                        Configuration.Save();
                    }
                }
            });
        }
        private void ConsoleSend(PlayerDb pdb)
        {
            Console.Write($"MixyTopTime => ", Console.ForegroundColor = ConsoleColor.Red);
            Console.Write($"{pdb.DisplayName} - {pdb.SteamId}", Console.ForegroundColor = ConsoleColor.Magenta);
            Console.Write($" = ", Console.ForegroundColor = ConsoleColor.Cyan);
            Console.WriteLine($"{Math.Ceiling(pdb.PlayingTime / 60)} Min.", Console.ForegroundColor = ConsoleColor.Yellow);
            Console.ResetColor();
        }
        private void ConsoleWarn(string msg)
        {
            Console.Write($"MixyTopTime => ", Console.ForegroundColor = ConsoleColor.Red);
            Console.WriteLine(msg, Console.ForegroundColor = ConsoleColor.Yellow);
            Console.ResetColor();
        }
    }
    #region Config
    public class Config : IRocketPluginConfiguration
    {
        public List<PlayerDb> PlayerDataBase = new List<PlayerDb>();
        public void LoadDefaults()
        {
            PlayerDataBase = new List<PlayerDb>();
        }
    }
    #endregion
    #region Models
    public class PlayerDb
    {
        [XmlAttribute]
        public ulong SteamId;
        [XmlAttribute]
        public string DisplayName;
        [XmlAttribute]
        public decimal PlayingTime;
        public PlayerDb() { }
        public PlayerDb(ulong SteamId, string DisplayName, decimal PlayingTime)
        {
            this.SteamId = SteamId;
            this.DisplayName = DisplayName;
            this.PlayingTime = PlayingTime;
        }
    }
    #endregion
}
