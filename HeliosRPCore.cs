// HeliosRPCore.cs
// uMod/Oxide plugin skeleton for Rust RP server (HeliosRP Core)
// Features scaffold: Config, Lang (ru/en), Data models, Services stubs, Commands, Basic CUI (Board + Licenses), NPC fallback stubs, Hooks.
// This is a compile-ready skeleton, but some parts are placeholders to be implemented per your final rules/other plugin dependencies.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HeliosRPCore", "Abdominomfala", "0.1.0")]
    [Description("RP Core: zones, raids, factions, licenses, police/court, economy, contracts board, NPC fallbacks (skeleton).")]
    public class HeliosRPCore : RustPlugin
    {
        #region === Constants / Permissions ===

        private const string PERM_ADMIN = "heliosrp.admin";
        private const string PERM_MAYOR = "heliosrp.mayor";
        private const string PERM_POLICE = "heliosrp.police";
        private const string PERM_JUDGE = "heliosrp.judge";
        private const string PERM_MEDIC = "heliosrp.medic";
        private const string PERM_FACTION_LEADER = "heliosrp.factionleader";

        private const string UI_ROOT = "HRP_UI_ROOT";
        private const string UI_BOARD = "HRP_UI_BOARD";
        private const string UI_LICENSES = "HRP_UI_LICENSES";

        #endregion

        #region === Config ===

        private PluginConfig _config;
        private string _transactionLogPath;

        [PluginReference]
        private Plugin Economics;

        private class PluginConfig
        {
            public GeneralSettings General = new GeneralSettings();
            public RaidWindowSettings RaidWindows = new RaidWindowSettings();
            public ZoneSettings Zones = new ZoneSettings();
            public LicenseSettings Licenses = new LicenseSettings();
            public EconomySettings Economy = new EconomySettings();
            public UISettings UI = new UISettings();
            public NPCSettings NPC = new NPCSettings();

            public class GeneralSettings
            {
                public string Timezone = "Europe/Berlin";
                public bool Debug = false;

                // Currency:
                public bool UseEconomicsPlugin = false; // optional integration
                public string CurrencyItemShortName = "scrap";
            }

            public class RaidWindowSettings
            {
                public bool Enabled = true;
                public bool AllowOfflineRaid = false;
                public bool BlockIfUnknownOwner = true;

                public List<RaidWindow> Windows = new List<RaidWindow>
                {
                    new RaidWindow { Day = "Wednesday", Start = "19:00", End = "22:00" },
                    new RaidWindow { Day = "Saturday",  Start = "19:00", End = "22:00" }
                };

                public class RaidWindow
                {
                    public string Day;   // e.g. "Wednesday"
                    public string Start; // "19:00"
                    public string End;   // "22:00"
                }
            }

            public class ZoneSettings
            {
                // Minimal: built-in zones are radius-based in this skeleton.
                // You can expand to polygons / ZoneManager integration later.
                public bool Enabled = true;
                public bool CitySafeBlocksCombat = true;
                public bool CitySafeBlocksRaidDamage = true;
                public bool CitySafeBlocksTheft = true;
                public bool CitySafeBlocksTurrets = true;
                public bool CitySafeBlocksTraps = true;
                public bool CitySafeBlocksLoot = true;
                public bool CitySafeBlocksExplosives = true;

                public bool SpecialEventBlocksCombat = true;
                public bool SpecialEventBlocksRaidDamage = true;
                public bool SpecialEventBlocksTheft = true;
                public bool SpecialEventBlocksTurrets = true;
                public bool SpecialEventBlocksTraps = true;
                public bool SpecialEventBlocksLoot = true;
                public bool SpecialEventBlocksExplosives = true;
            }

            public class LicenseSettings
            {
                public LicenseDef Trade = new LicenseDef { Cost = 150, DurationDays = 7 };
                public LicenseDef Guard = new LicenseDef { Cost = 200, DurationDays = 7 };
                public LicenseDef WeaponL1 = new LicenseDef { Cost = 100, DurationDays = 7 };
                public LicenseDef WeaponL2 = new LicenseDef { Cost = 200, DurationDays = 7 };
                public LicenseDef WeaponL3 = new LicenseDef { Cost = 400, DurationDays = 7 };
                public LicenseDef TurretPermit = new LicenseDef { Cost = 300, DurationDays = 7 };
                public List<int> ExpireNoticeHours = new List<int> { 24, 6 };

                public class LicenseDef
                {
                    public int Cost;
                    public int DurationDays;
                }
            }

            public class EconomySettings
            {
                public bool Enabled = true;
                public int BusinessRegistrationCost = 200;
                public int BusinessRegistrationDurationDays = 7;
                public int MedicInsuranceCost = 250; // weekly
                public int MedicInsuranceDurationDays = 7;
                public float BusinessTaxRate = 0.10f; // 10% (if implementing turnover)
            }

            public class UISettings
            {
                public bool Enabled = true;
                public string PanelColor = "0 0 0 0.85";
                public string AccentColor = "0.2 0.6 0.9 0.9";
                public string DangerColor = "0.8 0.2 0.2 0.9";
            }

            public class NPCSettings
            {
                public bool Enabled = true;
                public bool UseHumanNPC = false;          // integration stub
                public bool FallbackTerminalMode = true;  // if no NPC plugin, use "terminal marker + UI"
                public List<NPCProfile> Profiles = new List<NPCProfile>
                {
                    new NPCProfile { Id="ao_clerk", Role="mayor", DisplayName="AO Clerk", Position="0 0 0", RotationY=180, FallbackOnly=true },
                    new NPCProfile { Id="police_disp", Role="police", DisplayName="Dispatcher", Position="0 0 0", RotationY=90, FallbackOnly=true },
                    new NPCProfile { Id="court_clerk", Role="judge", DisplayName="Court Clerk", Position="0 0 0", RotationY=180, FallbackOnly=true },
                    new NPCProfile { Id="medic_recept", Role="medic", DisplayName="Medic Reception", Position="0 0 0", RotationY=180, FallbackOnly=true },
                    new NPCProfile { Id="board_clerk", Role="board", DisplayName="Board Clerk", Position="0 0 0", RotationY=180, FallbackOnly=true }
                };

                public class NPCProfile
                {
                    public string Id;
                    public string Role; // mayor/police/judge/medic/board
                    public string DisplayName;
                    public string Position; // "x y z"
                    public float RotationY;
                    public bool FallbackOnly;
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
                if (_config == null) throw new Exception("Config is null");
            }
            catch
            {
                PrintWarning("Config file is invalid; creating new default config.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        #endregion

        #region === Lang ===

        private void RegisterLang()
        {
            var en = new Dictionary<string, string>
            {
                ["Prefix"] = "[HeliosRP] ",
                ["NoPermission"] = "You don't have permission to use this command.",
                ["PlayerNotFound"] = "Player not found: {0}",
                ["UI_Title_Board"] = "Contract Board",
                ["UI_Title_Licenses"] = "Licenses",
                ["UI_Close"] = "Close",
                ["UI_Take"] = "Take",
                ["UI_Info"] = "Info",
                ["UI_Buy"] = "Buy",
                ["UI_Renew"] = "Renew",
                ["UI_ContractStatus_OPEN"] = "OPEN",
                ["UI_ContractStatus_TAKEN"] = "TAKEN",
                ["UI_ContractStatus_COMPLETED"] = "COMPLETED",
                ["UI_ContractStatus_DISPUTED"] = "DISPUTED",

                ["ZoneCombatBlocked"] = "Combat is blocked here (safe zone).",
                ["ZoneLootBlocked"] = "Looting is blocked in this zone.",
                ["ZoneTheftBlocked"] = "Theft is blocked in this zone.",
                ["ZoneExplosivesBlocked"] = "Explosives are blocked in this zone.",
                ["ZoneTurretsBlocked"] = "Turrets are blocked in this zone.",
                ["ZoneTrapsBlocked"] = "Traps are blocked in this zone.",
                ["ZoneRaidBlocked"] = "Raid damage is blocked in this zone.",
                ["RaidWindowClosed"] = "Raid window is closed. Next: {0}",
                ["RaidNoBasis"] = "No raiding basis. Required: WAR/CONTRACT/WARRANT/RETALIATION.",
                ["RaidNoOwner"] = "Owner could not be determined; raiding is blocked by server rules.",

                ["LicenseBought"] = "License {0} purchased. Valid until: {1}",
                ["LicenseExpired"] = "License {0} has expired.",
                ["LicenseExpiringSoon"] = "License {0} expires in {1}h.",
                ["LicenseRequired"] = "Required license: {0}",
                ["NotEnoughMoney"] = "Not enough funds. Required: {0}",
                ["PayUsage"] = "Usage: /pay <player> <amount>",
                ["PayInvalidAmount"] = "Invalid amount.",
                ["PaySelf"] = "You cannot pay yourself.",
                ["PaySent"] = "You sent {0} to {1}.",
                ["PayReceived"] = "You received {0} from {1}.",
                ["InsurancePurchased"] = "Medical insurance active until: {0}",
                ["BusinessRegistered"] = "Business registration active until: {0}",
                ["BusinessTaxCharged"] = "Weekly business tax charged: {0}",
                ["BusinessTaxFailed"] = "Weekly business tax could not be charged: {0}",

                ["ContractCreated"] = "Contract #{0} created. Reward: {1} scrap.",
                ["ContractTaken"] = "You have taken contract #{0}.",
                ["ContractCompleted"] = "Contract #{0} completed. Payment sent.",
                ["ContractDisputed"] = "Contract #{0} dispute sent to court.",

                ["NPC_FallbackNotice"] = "This role is currently unfilled — handled by an NPC.",
                ["NPC_GoToPlayerRole"] = "A player with this role is online: {0}. Please contact them IC.",
            };

            var ru = new Dictionary<string, string>
            {
                ["Prefix"] = "[HeliosRP] ",
                ["NoPermission"] = "У вас нет прав на эту команду.",
                ["PlayerNotFound"] = "Игрок не найден: {0}",
                ["UI_Title_Board"] = "Доска заказов",
                ["UI_Title_Licenses"] = "Лицензии",
                ["UI_Close"] = "Закрыть",
                ["UI_Take"] = "Взять",
                ["UI_Info"] = "Инфо",
                ["UI_Buy"] = "Купить",
                ["UI_Renew"] = "Продлить",
                ["UI_ContractStatus_OPEN"] = "ОТКРЫТ",
                ["UI_ContractStatus_TAKEN"] = "ВЗЯТ",
                ["UI_ContractStatus_COMPLETED"] = "ЗАВЕРШЕН",
                ["UI_ContractStatus_DISPUTED"] = "СПОР",

                ["ZoneCombatBlocked"] = "Здесь запрещены атаки и стрельба (безопасная зона).",
                ["ZoneLootBlocked"] = "Лут запрещен в этой зоне.",
                ["ZoneTheftBlocked"] = "Кража запрещена в этой зоне.",
                ["ZoneExplosivesBlocked"] = "В этой зоне запрещены взрывчатые.",
                ["ZoneTurretsBlocked"] = "В этой зоне запрещены турели.",
                ["ZoneTrapsBlocked"] = "В этой зоне запрещены ловушки.",
                ["ZoneRaidBlocked"] = "В этой зоне запрещен урон для рейда.",
                ["RaidWindowClosed"] = "Рейд-окно закрыто. Следующее: {0}",
                ["RaidNoBasis"] = "Нет основания для рейда. Нужно: WAR/CONTRACT/WARRANT/RETALIATION.",
                ["RaidNoOwner"] = "Владелец не определён; рейд заблокирован правилами сервера.",

                ["LicenseBought"] = "Лицензия {0} куплена. Действует до: {1}",
                ["LicenseExpired"] = "Лицензия {0} истекла.",
                ["LicenseExpiringSoon"] = "Лицензия {0} истекает через {1}ч.",
                ["LicenseRequired"] = "Требуется лицензия: {0}",
                ["NotEnoughMoney"] = "Недостаточно средств. Нужно: {0}",
                ["PayUsage"] = "Использование: /pay <player> <amount>",
                ["PayInvalidAmount"] = "Некорректная сумма.",
                ["PaySelf"] = "Нельзя платить самому себе.",
                ["PaySent"] = "Вы отправили {0} игроку {1}.",
                ["PayReceived"] = "Вы получили {0} от {1}.",
                ["InsurancePurchased"] = "Страховка медиков активна до: {0}",
                ["BusinessRegistered"] = "Регистрация бизнеса активна до: {0}",
                ["BusinessTaxCharged"] = "Еженедельный налог начислен: {0}",
                ["BusinessTaxFailed"] = "Еженедельный налог не удалось списать: {0}",

                ["ContractCreated"] = "Заказ #{0} создан. Награда: {1} scrap.",
                ["ContractTaken"] = "Вы взяли заказ #{0}.",
                ["ContractCompleted"] = "Заказ #{0} завершён. Выплата произведена.",
                ["ContractDisputed"] = "Спор по заказу #{0} отправлен в суд.",

                ["NPC_FallbackNotice"] = "Роль временно не занята игроком — обслуживает НПС.",
                ["NPC_GoToPlayerRole"] = "Игрок с этой ролью онлайн: {0}. Обратитесь к нему IC.",
            };

            lang.RegisterMessages(en, this, "en");
            lang.RegisterMessages(ru, this, "ru");
        }

        private string L(string key, BasePlayer player = null, params object[] args)
        {
            var msg = lang.GetMessage(key, this, player?.UserIDString);
            if (args != null && args.Length > 0) msg = string.Format(msg, args);
            return msg;
        }

        private void Reply(BasePlayer player, string key, params object[] args)
        {
            player.ChatMessage(L("Prefix", player) + L(key, player, args));
        }

        private bool IsFactionLeader(BasePlayer player, Faction faction)
        {
            if (player == null || faction == null) return false;
            if (faction.LeaderId == player.userID) return true;
            return permission.UserHasPermission(player.UserIDString, PERM_FACTION_LEADER)
                || permission.UserHasPermission(player.UserIDString, PERM_ADMIN);
        }

        #endregion

        #region === Data Store ===

        private DataStore _store;

        private class DataStore
        {
            public Dictionary<ulong, PlayerProfile> Players = new Dictionary<ulong, PlayerProfile>();
            public Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();
            public Dictionary<int, Contract> Contracts = new Dictionary<int, Contract>();
            public Dictionary<int, CaseFile> Cases = new Dictionary<int, CaseFile>();
            public Dictionary<string, War> Wars = new Dictionary<string, War>();
            public Dictionary<string, Zone> Zones = new Dictionary<string, Zone>();
            public Treasury Treasury = new Treasury();

            public int NextContractId = 1;
            public int NextCaseId = 1;
        }

        private class Treasury
        {
            public int BalanceScrap = 0;
            public List<Transaction> RecentTransactions = new List<Transaction>();
        }

        private class Transaction
        {
            public string Id = Guid.NewGuid().ToString("N");
            public string From;
            public string To;
            public int Amount;
            public string Reason;
            public long CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private class PlayerProfile
        {
            public ulong SteamId;
            public string LastName;
            public string FactionId;
            public HashSet<string> Roles = new HashSet<string>(); // "mayor/police/judge/medic" etc
            public List<LicenseEntry> Licenses = new List<LicenseEntry>();
            public long InsuranceUntilUnix;
            public long BusinessUntilUnix;
            public long CityBanUntilUnix;
            public int Strikes;
            public long LastDeathUnix;

            public bool HasRole(string role) => Roles != null && Roles.Contains(role);
        }

        private class LicenseEntry
        {
            public string Type; // Trade/Guard/WeaponL1/WeaponL2/WeaponL3/TurretPermit
            public long ExpiresAtUnix;
            public List<int> ExpireNotifiedHours = new List<int>();
        }

        private class Faction
        {
            public string Id;
            public string Name;
            public string Tag;
            public string Color;
            public string Description;
            public string Goals;
            public ulong LeaderId;
            public Dictionary<ulong, string> Members = new Dictionary<ulong, string>(); // steamId -> rank
        }

        private enum ContractType { DELIVERY, GUARD, BUILD, INVESTIGATE, BOUNTY, RAID }
        private enum ContractStatus { OPEN, TAKEN, COMPLETED, CANCELLED, DISPUTED }

        private class Contract
        {
            public int Id;
            public string Title;
            public string Description;
            public ulong CustomerId;
            public ulong ContractorId; // 0 if none
            public int Reward;
            public int Deposit;
            public ContractType Type;
            public ContractStatus Status;
            public long CreatedAtUnix;
            public long DueAtUnix;
            public string LocationHint;
            public ulong TargetOwnerId;
        }

        private enum CaseStatus { OPEN, HEARING_SCHEDULED, VERDICT, CLOSED }

        private class CaseFile
        {
            public int Id;
            public ulong SuspectId;
            public ulong ComplainantId;
            public ulong JudgeId;
            public ulong ProsecutorId;
            public List<string> Charges = new List<string>();
            public List<string> Evidence = new List<string>();
            public CaseStatus Status;
            public long CreatedAtUnix;
            public long ScheduleAtUnix;
            public Verdict Verdict;
            public bool WarrantActive;
            public ulong WarrantGrantedToId;
            public ulong WarrantTargetId;
            public long WarrantExpiresAtUnix;
            public bool RetaliationActive;
            public ulong RetaliationGrantedToId;
            public ulong RetaliationTargetId;
            public long RetaliationExpiresAtUnix;
        }

        private enum WarStatus { DECLARED, ACTIVE, ENDED }

        private class War
        {
            public string Id;
            public string AttackerFactionId;
            public string DefenderFactionId;
            public WarStatus Status;
            public long StartAtUnix;
            public long EndAtUnix;
        }

        private class Verdict
        {
            public int FineToTreasury;
            public int FineToVictim;
            public int JailMinutes;
            public List<string> Confiscations = new List<string>();
            public List<LicenseBan> LicenseBans = new List<LicenseBan>();
            public int CityBanMinutes;
        }

        private class LicenseBan
        {
            public string Type;
            public int Minutes;
        }

        private enum ZoneType { CITY_SAFE, SUBURB, WILD, SPECIAL_EVENT }
        private enum ZoneShape { RADIUS /* polygon later */ }
        [Flags]
        private enum ZoneBlockFlags
        {
            None = 0,
            Combat = 1 << 0,
            Raid = 1 << 1,
            Theft = 1 << 2,
            Turrets = 1 << 3,
            Traps = 1 << 4,
            Loot = 1 << 5,
            Explosives = 1 << 6
        }

        private class Zone
        {
            public string Id;
            public ZoneType Type;
            public ZoneShape Shape;
            public Vector3 Center;
            public float Radius;
            public int Priority;
        }

        private DynamicConfigFile _dataFile;

        private void LoadData()
        {
            _dataFile = Interface.Oxide.DataFileSystem.GetFile(Name);
            try
            {
                _store = _dataFile.ReadObject<DataStore>() ?? new DataStore();
            }
            catch
            {
                PrintWarning("Data file invalid; creating new store.");
                _store = new DataStore();
            }
        }

        private void SaveData()
        {
            _dataFile?.WriteObject(_store);
        }

        private string BuildWarKey(string attackerFactionId, string defenderFactionId)
        {
            return $"{attackerFactionId}:{defenderFactionId}";
        }

        private bool TryGetWarBetween(string factionA, string factionB, out War war)
        {
            war = null;
            if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return false;

            if (_store.Wars.TryGetValue(BuildWarKey(factionA, factionB), out war)) return true;
            if (_store.Wars.TryGetValue(BuildWarKey(factionB, factionA), out war)) return true;
            return false;
        }

        #endregion

        #region === Services (Stubs) ===

        private ZoneService _zones;
        private EconomyService _economy;
        private LicenseService _licenses;
        private FactionService _factions;
        private RaidAuthService _raidAuth;
        private ContractService _contracts;
        private CourtService _court;
        private NPCService _npc;

        private void InitServices()
        {
            _zones = new ZoneService(this);
            _economy = new EconomyService(this);
            _licenses = new LicenseService(this);
            _factions = new FactionService(this);
            _raidAuth = new RaidAuthService(this);
            _contracts = new ContractService(this);
            _court = new CourtService(this);
            _npc = new NPCService(this);
        }

        private class ZoneService
        {
            private readonly HeliosRPCore _p;
            public ZoneService(HeliosRPCore p) { _p = p; }

            public bool IsCitySafe(Vector3 pos)
            {
                return GetZoneType(pos) == ZoneType.CITY_SAFE || GetZoneType(pos) == ZoneType.SPECIAL_EVENT;
            }

            public ZoneType GetZoneType(Vector3 pos)
            {
                var zone = GetBestZone(pos);
                return zone?.Type ?? ZoneType.WILD;
            }

            public ZoneBlockFlags GetZoneFlags(Vector3 pos)
            {
                var zone = GetBestZone(pos);
                if (zone == null) return ZoneBlockFlags.None;
                return GetFlagsForZoneType(zone.Type);
            }

            private Zone GetBestZone(Vector3 pos)
            {
                Zone best = null;
                int bestPr = int.MinValue;

                foreach (var z in _p._store.Zones.Values)
                {
                    if (z.Shape != ZoneShape.RADIUS) continue;
                    if (Vector3.Distance(pos, z.Center) > z.Radius) continue;

                    var effectivePriority = z.Priority + (z.Type == ZoneType.SPECIAL_EVENT ? 10000 : 0);
                    if (effectivePriority > bestPr)
                    {
                        bestPr = effectivePriority;
                        best = z;
                    }
                }

                return best;
            }

            private ZoneBlockFlags GetFlagsForZoneType(ZoneType type)
            {
                switch (type)
                {
                    case ZoneType.CITY_SAFE:
                        return BuildFlags(
                            _p._config.Zones.CitySafeBlocksCombat,
                            _p._config.Zones.CitySafeBlocksRaidDamage,
                            _p._config.Zones.CitySafeBlocksTheft,
                            _p._config.Zones.CitySafeBlocksTurrets,
                            _p._config.Zones.CitySafeBlocksTraps,
                            _p._config.Zones.CitySafeBlocksLoot,
                            _p._config.Zones.CitySafeBlocksExplosives);
                    case ZoneType.SPECIAL_EVENT:
                        return BuildFlags(
                            _p._config.Zones.SpecialEventBlocksCombat,
                            _p._config.Zones.SpecialEventBlocksRaidDamage,
                            _p._config.Zones.SpecialEventBlocksTheft,
                            _p._config.Zones.SpecialEventBlocksTurrets,
                            _p._config.Zones.SpecialEventBlocksTraps,
                            _p._config.Zones.SpecialEventBlocksLoot,
                            _p._config.Zones.SpecialEventBlocksExplosives);
                    default:
                        return ZoneBlockFlags.None;
                }
            }

            private static ZoneBlockFlags BuildFlags(
                bool combat,
                bool raid,
                bool theft,
                bool turrets,
                bool traps,
                bool loot,
                bool explosives)
            {
                var flags = ZoneBlockFlags.None;
                if (combat) flags |= ZoneBlockFlags.Combat;
                if (raid) flags |= ZoneBlockFlags.Raid;
                if (theft) flags |= ZoneBlockFlags.Theft;
                if (turrets) flags |= ZoneBlockFlags.Turrets;
                if (traps) flags |= ZoneBlockFlags.Traps;
                if (loot) flags |= ZoneBlockFlags.Loot;
                if (explosives) flags |= ZoneBlockFlags.Explosives;
                return flags;
            }
        }

        private class EconomyService
        {
            private readonly HeliosRPCore _p;
            public EconomyService(HeliosRPCore p) { _p = p; }

            private bool UseEconomics => _p._config.General.UseEconomicsPlugin && _p.Economics != null;

            public bool TryWithdraw(ulong steamId, int amount)
            {
                if (amount <= 0) return true;

                if (UseEconomics)
                {
                    var balObj = _p.Economics.Call("Balance", steamId);
                    var bal = balObj != null ? Convert.ToDouble(balObj) : 0d;
                    if (bal < amount) return false;
                    _p.Economics.Call("Withdraw", steamId, (double)amount);
                    return true;
                }

                var player = BasePlayer.FindByID(steamId);
                if (player == null || !player.IsConnected) return false;
                return TryChargeScrap(player, amount);
            }

            public bool TryDeposit(ulong steamId, int amount)
            {
                if (amount <= 0) return true;

                if (UseEconomics)
                {
                    _p.Economics.Call("Deposit", steamId, (double)amount);
                    return true;
                }

                var player = BasePlayer.FindByID(steamId);
                if (player == null || !player.IsConnected) return false;
                GiveScrap(player, amount);
                return true;
            }

            public bool TryTransfer(ulong fromId, ulong toId, int amount)
            {
                if (!TryWithdraw(fromId, amount)) return false;
                if (!TryDeposit(toId, amount))
                {
                    TryDeposit(fromId, amount);
                    return false;
                }
                return true;
            }

            public bool TryChargeScrap(BasePlayer player, int amount)
            {
                if (amount <= 0) return true;
                var itemDef = ItemManager.FindItemDefinition(_p._config.General.CurrencyItemShortName);
                if (itemDef == null) return false;

                int have = player.inventory.GetAmount(itemDef.itemid);
                if (have < amount) return false;

                player.inventory.Take(null, itemDef.itemid, amount);
                player.Command("note.inv", itemDef.itemid, -amount); // client update (best effort)
                return true;
            }

            public void GiveScrap(BasePlayer player, int amount)
            {
                if (amount <= 0) return;
                var itemDef = ItemManager.FindItemDefinition(_p._config.General.CurrencyItemShortName);
                if (itemDef == null) return;

                var item = ItemManager.Create(itemDef, amount);
                if (!player.inventory.GiveItem(item))
                    item.Drop(player.transform.position, Vector3.up);
            }

            public void TreasuryAdd(int amount, string reason, string from = "system")
            {
                _p._store.Treasury.BalanceScrap += amount;
                _p.RecordTransaction(from, "treasury", amount, reason);
            }
        }

        private class LicenseService
        {
            private readonly HeliosRPCore _p;
            public LicenseService(HeliosRPCore p) { _p = p; }

            public bool HasLicense(ulong steamId, string type)
            {
                var prof = _p.GetOrCreateProfile(steamId);
                var now = NowUnix();
                return prof.Licenses.Any(l => l.Type.Equals(type, StringComparison.OrdinalIgnoreCase) && l.ExpiresAtUnix > now);
            }

            public void GrantOrRenew(ulong steamId, string type, int durationDays)
            {
                var prof = _p.GetOrCreateProfile(steamId);
                var now = NowUnix();
                var existing = prof.Licenses.FirstOrDefault(l => l.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
                var newExp = DateTimeOffset.UtcNow.AddDays(durationDays).ToUnixTimeSeconds();

                if (existing == null)
                    prof.Licenses.Add(new LicenseEntry { Type = type, ExpiresAtUnix = newExp, ExpireNotifiedHours = new List<int>() });
                else
                {
                    existing.ExpiresAtUnix = Math.Max(existing.ExpiresAtUnix, now) + durationDays * 86400;
                    existing.ExpireNotifiedHours?.Clear();
                }
            }

            public void ExpireTick()
            {
                var now = NowUnix();
                var noticeHours = _p._config.Licenses.ExpireNoticeHours ?? new List<int>();
                var sortedNoticeHours = noticeHours
                    .Where(h => h > 0)
                    .Distinct()
                    .OrderByDescending(h => h)
                    .ToList();

                foreach (var kv in _p._store.Players)
                {
                    var prof = kv.Value;
                    if (prof.Licenses == null) continue;

                    var expired = new List<LicenseEntry>();
                    foreach (var license in prof.Licenses)
                    {
                        if (license.ExpiresAtUnix <= now)
                        {
                            expired.Add(license);
                            continue;
                        }

                        if (sortedNoticeHours.Count == 0) continue;
                        if (license.ExpireNotifiedHours == null)
                            license.ExpireNotifiedHours = new List<int>();

                        var secondsLeft = license.ExpiresAtUnix - now;
                        foreach (var hours in sortedNoticeHours)
                        {
                            if (secondsLeft <= hours * 3600L && !license.ExpireNotifiedHours.Contains(hours))
                            {
                                var player = BasePlayer.FindByID(prof.SteamId);
                                if (player != null && player.IsConnected)
                                    _p.Reply(player, "LicenseExpiringSoon", license.Type, hours);
                                license.ExpireNotifiedHours.Add(hours);
                            }
                        }
                    }

                    if (expired.Count > 0)
                    {
                        var player = BasePlayer.FindByID(prof.SteamId);
                        if (player != null && player.IsConnected)
                        {
                            foreach (var license in expired)
                                _p.Reply(player, "LicenseExpired", license.Type);
                        }
                        foreach (var license in expired)
                            prof.Licenses.Remove(license);
                    }
                }
            }
        }

        private class FactionService
        {
            private readonly HeliosRPCore _p;
            public FactionService(HeliosRPCore p) { _p = p; }

            public string GetFactionId(ulong steamId)
            {
                var prof = _p.GetOrCreateProfile(steamId);
                return prof.FactionId;
            }

            public Faction GetFaction(ulong steamId)
            {
                var fid = GetFactionId(steamId);
                if (string.IsNullOrEmpty(fid)) return null;
                _p._store.Factions.TryGetValue(fid, out var f);
                return f;
            }
        }

        private class RaidAuthService
        {
            private readonly HeliosRPCore _p;
            public string LastBasis { get; private set; } = "";

            public RaidAuthService(HeliosRPCore p) { _p = p; }

            public bool IsRaidWindowNow(out string nextInfo)
            {
                nextInfo = "N/A";
                if (!_p._config.RaidWindows.Enabled) return true;

                // Simple local server time. If you want exact timezone handling, you can implement with TimeZoneInfo.
                // Skeleton: compares DayOfWeek + HH:mm.
                var now = DateTime.Now;
                var dayName = now.DayOfWeek.ToString();
                var hhmm = now.ToString("HH:mm");

                foreach (var w in _p._config.RaidWindows.Windows)
                {
                    if (!string.Equals(w.Day, dayName, StringComparison.OrdinalIgnoreCase)) continue;
                    if (IsBetween(hhmm, w.Start, w.End)) return true;
                }

                // next window info (best effort)
                nextInfo = BuildNextWindowInfo(now);
                return false;
            }

            public bool HasRaidPermission(ulong attackerId, ulong targetOwnerId)
            {
                LastBasis = "";

                // 1) WAR (stub: implement using stored wars)
                if (IsAtWar(attackerId, targetOwnerId))
                {
                    LastBasis = "WAR";
                    return true;
                }

                // 2) WARRANT (stub: implement in CourtService)
                if (_p._court.HasActiveWarrant(attackerId, targetOwnerId))
                {
                    LastBasis = "WARRANT";
                    return true;
                }

                // 3) CONTRACT (stub: implement in ContractService)
                if (_p._contracts.HasActiveRaidContract(attackerId, targetOwnerId))
                {
                    LastBasis = "CONTRACT";
                    return true;
                }

                // 4) RETALIATION (stub: implement in CourtService)
                if (_p._court.HasRetaliationPermit(attackerId, targetOwnerId))
                {
                    LastBasis = "RETALIATION";
                    return true;
                }

                return false;
            }

            private bool IsAtWar(ulong a, ulong b)
            {
                var factionA = _p._factions.GetFactionId(a);
                var factionB = _p._factions.GetFactionId(b);
                if (string.IsNullOrEmpty(factionA) || string.IsNullOrEmpty(factionB)) return false;

                if (_p.TryGetWarBetween(factionA, factionB, out var war))
                    return war.Status == WarStatus.ACTIVE;

                return false;
            }

            private static bool IsBetween(string t, string start, string end)
            {
                // Lexicographical works for "HH:mm"
                return string.CompareOrdinal(t, start) >= 0 && string.CompareOrdinal(t, end) <= 0;
            }

            private string BuildNextWindowInfo(DateTime now)
            {
                // Minimal best-effort; implement proper next-occurrence if needed.
                return "See /rp or server rules";
            }
        }

        private class ContractService
        {
            private readonly HeliosRPCore _p;
            public ContractService(HeliosRPCore p) { _p = p; }

            public Contract Create(ulong customerId, string title, string desc, int reward, ContractType type, ulong targetOwnerId = 0)
            {
                var id = _p._store.NextContractId++;
                var c = new Contract
                {
                    Id = id,
                    Title = title,
                    Description = desc,
                    CustomerId = customerId,
                    Reward = reward,
                    Deposit = reward, // for MVP: deposit equals reward; you can change
                    Type = type,
                    Status = ContractStatus.OPEN,
                    CreatedAtUnix = NowUnix(),
                    TargetOwnerId = targetOwnerId
                };
                _p._store.Contracts[id] = c;
                return c;
            }

            public bool Take(int id, ulong contractorId)
            {
                if (!_p._store.Contracts.TryGetValue(id, out var c)) return false;
                if (c.Status != ContractStatus.OPEN) return false;

                c.ContractorId = contractorId;
                c.Status = ContractStatus.TAKEN;
                return true;
            }

            public bool HasActiveRaidContract(ulong attackerId, ulong targetOwnerId)
            {
                var now = NowUnix();
                foreach (var c in _p._store.Contracts.Values)
                {
                    if (c.Type != ContractType.RAID) continue;
                    if (c.Status != ContractStatus.TAKEN) continue;
                    if (c.ContractorId != attackerId) continue;
                    if (c.TargetOwnerId != targetOwnerId) continue;
                    if (c.DueAtUnix != 0 && c.DueAtUnix <= now) continue;
                    return true;
                }
                return false;
            }
        }

        private class CourtService
        {
            private readonly HeliosRPCore _p;
            public CourtService(HeliosRPCore p) { _p = p; }

            public bool HasActiveWarrant(ulong attackerId, ulong targetOwnerId)
            {
                var now = NowUnix();
                foreach (var c in _p._store.Cases.Values)
                {
                    if (!c.WarrantActive) continue;
                    if (c.Status == CaseStatus.CLOSED) continue;
                    if (c.WarrantGrantedToId != attackerId) continue;
                    if (c.WarrantTargetId != targetOwnerId) continue;
                    if (c.WarrantExpiresAtUnix != 0 && c.WarrantExpiresAtUnix <= now) continue;
                    return true;
                }
                return false;
            }

            public bool HasRetaliationPermit(ulong attackerId, ulong targetOwnerId)
            {
                var now = NowUnix();
                foreach (var c in _p._store.Cases.Values)
                {
                    if (!c.RetaliationActive) continue;
                    if (c.Status == CaseStatus.CLOSED) continue;
                    if (c.RetaliationGrantedToId != attackerId) continue;
                    if (c.RetaliationTargetId != targetOwnerId) continue;
                    if (c.RetaliationExpiresAtUnix != 0 && c.RetaliationExpiresAtUnix <= now) continue;
                    return true;
                }
                return false;
            }
        }

        private class NPCService
        {
            private readonly HeliosRPCore _p;
            public NPCService(HeliosRPCore p) { _p = p; }

            public void SpawnFallbacksIfNeeded()
            {
                if (!_p._config.NPC.Enabled) return;

                // SKELETON:
                // - If UseHumanNPC is true: call HumanNPC API to spawn configured NPCs (not implemented).
                // - Else if FallbackTerminalMode: you can later spawn a "terminal marker" (e.g., static entity) and use raycast+Use button.
                // For now, we only log configured profiles.
                if (_p._config.General.Debug)
                {
                    foreach (var n in _p._config.NPC.Profiles)
                        _p.Puts($"[NPC] profile: {n.Id} role={n.Role} pos={n.Position} fallbackOnly={n.FallbackOnly}");
                }
            }

            public bool IsRoleFilledByPlayer(string role, out string playerName)
            {
                playerName = null;

                // Example role mapping to permission groups:
                // mayor => PERM_MAYOR; police => PERM_POLICE; judge => PERM_JUDGE; medic => PERM_MEDIC
                string perm = role switch
                {
                    "mayor" => PERM_MAYOR,
                    "police" => PERM_POLICE,
                    "judge" => PERM_JUDGE,
                    "medic" => PERM_MEDIC,
                    _ => null
                };
                if (perm == null) return false;

                foreach (var p in BasePlayer.activePlayerList)
                {
                    if (_p.permission.UserHasPermission(p.UserIDString, perm))
                    {
                        playerName = p.displayName;
                        return true;
                    }
                }
                return false;
            }
        }

        #endregion

        #region === Hooks ===

        private void Init()
        {
            permission.RegisterPermission(PERM_ADMIN, this);
            permission.RegisterPermission(PERM_MAYOR, this);
            permission.RegisterPermission(PERM_POLICE, this);
            permission.RegisterPermission(PERM_JUDGE, this);
            permission.RegisterPermission(PERM_MEDIC, this);
            permission.RegisterPermission(PERM_FACTION_LEADER, this);

            RegisterLang();
            LoadData();
            InitServices();
            _transactionLogPath = Path.Combine(Interface.Oxide.DataFileSystem.Directory, $"{Name}_transactions.jsonl");
        }

        private void OnServerInitialized()
        {
            // Ensure some default zones exist for a fresh server (optional).
            if (_store.Zones.Count == 0)
            {
                // Example placeholder city safe zone at 0,0,0 radius 200. You MUST change coordinates.
                _store.Zones["city_safe"] = new Zone
                {
                    Id = "city_safe",
                    Type = ZoneType.CITY_SAFE,
                    Shape = ZoneShape.RADIUS,
                    Center = Vector3.zero,
                    Radius = 200f,
                    Priority = 100
                };
                SaveData();
                PrintWarning("Created default CITY_SAFE zone at 0,0,0 radius 200. Update in data or add admin commands to set properly.");
            }

            // Timers
            timer.Every(60f, () =>
            {
                _licenses.ExpireTick();
                SaveData(); // simple; later you may autosave less frequently
            });

            timer.Every(604800f, () =>
            {
                ApplyWeeklyBusinessTax();
                SaveData();
            });

            // Spawn NPC fallbacks (stub)
            _npc.SpawnFallbacksIfNeeded();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            var prof = GetOrCreateProfile(player.userID);
            prof.LastName = player.displayName;
            SaveData();
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            var prof = GetOrCreateProfile(player.userID);
            prof.LastDeathUnix = NowUnix();
            SaveData();
        }

        object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (attacker == null || info == null) return null;

            if (IsCitySafeWeaponRestricted(attacker))
            {
                Reply(attacker, "LicenseRequired", GetWeaponLicenseRequirementText());
                return false;
            }

            // Block combat in zone
            if (_config.Zones.Enabled)
            {
                var attackerFlags = _zones.GetZoneFlags(attacker.transform.position);
                var targetFlags = ZoneBlockFlags.None;
                var targetEntity = info.HitEntity;
                if (targetEntity != null)
                    targetFlags = _zones.GetZoneFlags(targetEntity.transform.position);

                if (HasFlag(attackerFlags, ZoneBlockFlags.Combat) || HasFlag(targetFlags, ZoneBlockFlags.Combat))
                {
                    Reply(attacker, "ZoneCombatBlocked");
                    return false;
                }
            }

            return null;
        }

        object OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || newItem == null) return null;
            if (!_config.Zones.Enabled) return null;
            if (_zones.GetZoneType(player.transform.position) != ZoneType.CITY_SAFE) return null;
            if (!IsWeaponItem(newItem)) return null;
            if (HasAnyWeaponLicense(player.userID)) return null;

            Reply(player, "LicenseRequired", GetWeaponLicenseRequirementText());
            player.UpdateActiveItem(0);
            return null;
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return null;
            var attacker = info.InitiatorPlayer;
            if (attacker == null) return null;

            // 1) Hard block any raid damage in zone
            if (_config.Zones.Enabled)
            {
                var entityFlags = _zones.GetZoneFlags(entity.transform.position);
                var attackerFlags = _zones.GetZoneFlags(attacker.transform.position);

                if ((HasFlag(entityFlags, ZoneBlockFlags.Raid) || HasFlag(attackerFlags, ZoneBlockFlags.Raid)))
                {
                    // If this is building/door damage via raid means, block.
                    if (IsRaidRelevantEntity(entity) && IsRaidDamage(info))
                    {
                        info.damageTypes = new DamageTypeList();
                        info.HitMaterial = 0;
                        Reply(attacker, "ZoneRaidBlocked");
                        return true;
                    }
                }
            }

            // 2) Raid window + basis checks
            if (IsRaidRelevantEntity(entity) && IsRaidDamage(info))
            {
                if (!_raidAuth.IsRaidWindowNow(out var nextInfo))
                {
                    info.damageTypes = new DamageTypeList();
                    Reply(attacker, "RaidWindowClosed", nextInfo);
                    return true;
                }

                var targetOwnerId = ResolveOwnerId(entity);
                if (targetOwnerId == 0 && _config.RaidWindows.BlockIfUnknownOwner)
                {
                    info.damageTypes = new DamageTypeList();
                    Reply(attacker, "RaidNoOwner");
                    return true;
                }

                if (targetOwnerId != 0 && !_raidAuth.HasRaidPermission(attacker.userID, targetOwnerId))
                {
                    info.damageTypes = new DamageTypeList();
                    Reply(attacker, "RaidNoBasis");
                    return true;
                }

                // Allowed -> pass through
            }

            return null;
        }

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            if (planner == null || go == null) return;
            var player = planner.GetOwnerPlayer();
            if (player == null) return;

            var entity = go.ToBaseEntity();
            if (entity == null) return;

            if (IsTurretEntity(entity) || IsTrapEntity(entity))
            {
                if (!_licenses.HasLicense(player.userID, "TurretPermit"))
                {
                    Reply(player, "LicenseRequired", "TurretPermit");
                    NextTick(() => entity.Kill());
                }
                return;
            }

            if (IsTradeEntity(entity))
            {
                if (!_licenses.HasLicense(player.userID, "Trade"))
                {
                    Reply(player, "LicenseRequired", "Trade");
                    NextTick(() => entity.Kill());
                }
            }
        }

        object OnExplosiveThrown(BasePlayer player, BaseEntity entity, Item item)
        {
            if (player == null) return null;
            if (!_config.Zones.Enabled) return null;

            var flags = _zones.GetZoneFlags(player.transform.position);
            if (HasFlag(flags, ZoneBlockFlags.Explosives))
            {
                Reply(player, "ZoneExplosivesBlocked");
                return false;
            }

            return null;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_config.Zones.Enabled) return;
            if (entity == null) return;

            var baseEntity = entity as BaseEntity;
            if (baseEntity == null) return;

            if (!IsTurretEntity(baseEntity) && !IsTrapEntity(baseEntity)) return;

            var flags = _zones.GetZoneFlags(baseEntity.transform.position);
            if (IsTurretEntity(baseEntity) && HasFlag(flags, ZoneBlockFlags.Turrets))
            {
                NotifyOwner(baseEntity, "ZoneTurretsBlocked");
                NextTick(() => baseEntity.Kill());
                return;
            }

            if (IsTrapEntity(baseEntity) && HasFlag(flags, ZoneBlockFlags.Traps))
            {
                NotifyOwner(baseEntity, "ZoneTrapsBlocked");
                NextTick(() => baseEntity.Kill());
            }
        }

        object CanLootEntity(BasePlayer player, BaseEntity target)
        {
            if (player == null || target == null) return null;
            if (!_config.Zones.Enabled) return null;

            var flags = _zones.GetZoneFlags(player.transform.position) | _zones.GetZoneFlags(target.transform.position);
            if (HasFlag(flags, ZoneBlockFlags.Loot))
            {
                Reply(player, "ZoneLootBlocked");
                return false;
            }

            if (HasFlag(flags, ZoneBlockFlags.Theft))
            {
                Reply(player, "ZoneTheftBlocked");
                return false;
            }

            return null;
        }

        private void OnLootEntity(BasePlayer player, BaseEntity target)
        {
            if (player == null || target == null) return;
            if (!_config.Zones.Enabled) return;

            var flags = _zones.GetZoneFlags(player.transform.position) | _zones.GetZoneFlags(target.transform.position);
            if (HasFlag(flags, ZoneBlockFlags.Loot) || HasFlag(flags, ZoneBlockFlags.Theft))
            {
                player.EndLooting();
            }
        }

        #endregion

        #region === Commands (Chat + Console for UI) ===

        [ChatCommand("rp")]
        private void CmdRP(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage(L("Prefix", player) + "Commands: /board, /licenses, /buylicense <type>, /pay <player> <amount>, /insurance buy, /business register, /war <declare|accept|status>");
        }

        [ChatCommand("war")]
        private void CmdWar(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage(L("Prefix", player) + "Usage: /war declare <factionId> | /war accept <factionId> | /war status");
                return;
            }

            var sub = args[0].ToLowerInvariant();
            var playerFaction = _factions.GetFaction(player.userID);

            switch (sub)
            {
                case "declare":
                    {
                        if (playerFaction == null)
                        {
                            player.ChatMessage(L("Prefix", player) + "You are not in a faction.");
                            return;
                        }

                        if (!IsFactionLeader(player, playerFaction))
                        {
                            Reply(player, "NoPermission");
                            return;
                        }

                        if (args.Length < 2)
                        {
                            player.ChatMessage(L("Prefix", player) + "Usage: /war declare <factionId>");
                            return;
                        }

                        var targetFactionId = args[1];
                        if (!_store.Factions.TryGetValue(targetFactionId, out var targetFaction))
                        {
                            player.ChatMessage(L("Prefix", player) + $"Faction not found: {targetFactionId}");
                            return;
                        }

                        if (targetFactionId == playerFaction.Id)
                        {
                            player.ChatMessage(L("Prefix", player) + "You cannot declare war on your own faction.");
                            return;
                        }

                        if (TryGetWarBetween(playerFaction.Id, targetFactionId, out var existing) && existing.Status != WarStatus.ENDED)
                        {
                            player.ChatMessage(L("Prefix", player) + $"War already exists with {targetFactionId}.");
                            return;
                        }

                        var key = BuildWarKey(playerFaction.Id, targetFactionId);
                        var war = new War
                        {
                            Id = key,
                            AttackerFactionId = playerFaction.Id,
                            DefenderFactionId = targetFactionId,
                            Status = WarStatus.DECLARED,
                            StartAtUnix = NowUnix(),
                            EndAtUnix = 0
                        };
                        _store.Wars[key] = war;
                        SaveData();

                        player.ChatMessage(L("Prefix", player) + $"War declared on {targetFactionId}. Awaiting acceptance.");
                        return;
                    }

                case "accept":
                    {
                        if (playerFaction == null)
                        {
                            player.ChatMessage(L("Prefix", player) + "You are not in a faction.");
                            return;
                        }

                        if (!IsFactionLeader(player, playerFaction))
                        {
                            Reply(player, "NoPermission");
                            return;
                        }

                        if (args.Length < 2)
                        {
                            player.ChatMessage(L("Prefix", player) + "Usage: /war accept <factionId>");
                            return;
                        }

                        var attackerFactionId = args[1];
                        var key = BuildWarKey(attackerFactionId, playerFaction.Id);
                        if (!_store.Wars.TryGetValue(key, out var war))
                        {
                            player.ChatMessage(L("Prefix", player) + $"No war declaration found from {attackerFactionId}.");
                            return;
                        }

                        if (war.Status == WarStatus.ACTIVE)
                        {
                            player.ChatMessage(L("Prefix", player) + "War is already active.");
                            return;
                        }

                        war.Status = WarStatus.ACTIVE;
                        war.StartAtUnix = NowUnix();
                        SaveData();

                        player.ChatMessage(L("Prefix", player) + $"War with {attackerFactionId} is now active.");
                        return;
                    }

                case "status":
                    {
                        if (playerFaction == null)
                        {
                            player.ChatMessage(L("Prefix", player) + "You are not in a faction.");
                            return;
                        }

                        var wars = _store.Wars.Values
                            .Where(w => w.AttackerFactionId == playerFaction.Id || w.DefenderFactionId == playerFaction.Id)
                            .ToList();

                        if (wars.Count == 0)
                        {
                            player.ChatMessage(L("Prefix", player) + "No wars for your faction.");
                            return;
                        }

                        player.ChatMessage(L("Prefix", player) + "War status:");
                        foreach (var war in wars)
                        {
                            var otherFactionId = war.AttackerFactionId == playerFaction.Id
                                ? war.DefenderFactionId
                                : war.AttackerFactionId;
                            player.ChatMessage($"- vs {otherFactionId}: {war.Status}");
                        }
                        return;
                    }

                default:
                    player.ChatMessage(L("Prefix", player) + "Usage: /war declare <factionId> | /war accept <factionId> | /war status");
                    return;
            }
        }

        [ChatCommand("board")]
        private void CmdBoard(BasePlayer player, string command, string[] args)
        {
            if (!_config.UI.Enabled) return;
            DestroyUI(player);
            UI_ShowBoard(player);
        }

        [ChatCommand("licenses")]
        private void CmdLicenses(BasePlayer player, string command, string[] args)
        {
            if (!_config.UI.Enabled) return;
            DestroyUI(player);
            UI_ShowLicenses(player);
        }

        [ChatCommand("buylicense")]
        private void CmdBuyLicense(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.ChatMessage(L("Prefix", player) + "Usage: /buylicense Trade|Guard|WeaponL1|WeaponL2|WeaponL3|TurretPermit");
                return;
            }

            var type = args[0];
            if (!TryGetLicenseDef(type, out var cost, out var days))
            {
                player.ChatMessage(L("Prefix", player) + "Unknown license type.");
                return;
            }

            if (!_economy.TryWithdraw(player.userID, cost))
            {
                Reply(player, "NotEnoughMoney", cost);
                return;
            }

            _licenses.GrantOrRenew(player.userID, type, days);
            SaveData();

            var exp = DateTimeOffset.UtcNow.AddDays(days).ToString("yyyy-MM-dd HH:mm");
            Reply(player, "LicenseBought", type, exp);
        }

        [ChatCommand("pay")]
        private void CmdPay(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                Reply(player, "PayUsage");
                return;
            }

            var target = FindOnlinePlayer(args[0]);
            if (target == null)
            {
                Reply(player, "PlayerNotFound", args[0]);
                return;
            }

            if (target.userID == player.userID)
            {
                Reply(player, "PaySelf");
                return;
            }

            if (!int.TryParse(args[1], out var amount) || amount <= 0)
            {
                Reply(player, "PayInvalidAmount");
                return;
            }

            if (!_economy.TryTransfer(player.userID, target.userID, amount))
            {
                Reply(player, "NotEnoughMoney", amount);
                return;
            }

            RecordTransaction(player.UserIDString, target.UserIDString, amount, "pay");
            Reply(player, "PaySent", amount, target.displayName);
            Reply(target, "PayReceived", amount, player.displayName);
        }

        [ChatCommand("insurance")]
        private void CmdInsurance(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1 || !args[0].Equals("buy", StringComparison.OrdinalIgnoreCase))
            {
                player.ChatMessage(L("Prefix", player) + "Usage: /insurance buy");
                return;
            }

            if (!_config.Economy.Enabled)
            {
                player.ChatMessage(L("Prefix", player) + "Economy is disabled.");
                return;
            }

            var cost = _config.Economy.MedicInsuranceCost;
            if (!_economy.TryWithdraw(player.userID, cost))
            {
                Reply(player, "NotEnoughMoney", cost);
                return;
            }

            var prof = GetOrCreateProfile(player.userID);
            var now = NowUnix();
            var duration = _config.Economy.MedicInsuranceDurationDays;
            prof.InsuranceUntilUnix = Math.Max(prof.InsuranceUntilUnix, now) + duration * 86400L;

            _economy.TreasuryAdd(cost, "medic_insurance", player.UserIDString);
            SaveData();

            var exp = DateTimeOffset.FromUnixTimeSeconds(prof.InsuranceUntilUnix).ToString("yyyy-MM-dd HH:mm");
            Reply(player, "InsurancePurchased", exp);
        }

        [ChatCommand("business")]
        private void CmdBusiness(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1 || !args[0].Equals("register", StringComparison.OrdinalIgnoreCase))
            {
                player.ChatMessage(L("Prefix", player) + "Usage: /business register");
                return;
            }

            if (!_config.Economy.Enabled)
            {
                player.ChatMessage(L("Prefix", player) + "Economy is disabled.");
                return;
            }

            var cost = _config.Economy.BusinessRegistrationCost;
            if (!_economy.TryWithdraw(player.userID, cost))
            {
                Reply(player, "NotEnoughMoney", cost);
                return;
            }

            var prof = GetOrCreateProfile(player.userID);
            var now = NowUnix();
            var duration = _config.Economy.BusinessRegistrationDurationDays;
            prof.BusinessUntilUnix = Math.Max(prof.BusinessUntilUnix, now) + duration * 86400L;

            _economy.TreasuryAdd(cost, "business_registration", player.UserIDString);
            SaveData();

            var exp = DateTimeOffset.FromUnixTimeSeconds(prof.BusinessUntilUnix).ToString("yyyy-MM-dd HH:mm");
            Reply(player, "BusinessRegistered", exp);
        }

        // UI commands
        [ConsoleCommand("hrp.ui")]
        private void CmdUI(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            // hrp.ui <panel>.<action> [args...]
            var sub = arg.GetString(0, "");
            if (string.IsNullOrEmpty(sub)) return;

            switch (sub)
            {
                case "close":
                    DestroyUI(player);
                    return;

                case "board.take":
                    {
                        var id = arg.GetInt(1, 0);
                        if (id <= 0) return;

                        if (_store.Contracts.TryGetValue(id, out var c) && c.Status == ContractStatus.OPEN)
                        {
                            if (_contracts.Take(id, player.userID))
                                Reply(player, "ContractTaken", id);
                        }
                        DestroyUI(player);
                        UI_ShowBoard(player);
                        return;
                    }

                case "licenses.buy":
                    {
                        var type = arg.GetString(1, "");
                        if (string.IsNullOrEmpty(type)) return;
                        // reuse buy logic
                        CmdBuyLicense(player, "buylicense", new[] { type });
                        DestroyUI(player);
                        UI_ShowLicenses(player);
                        return;
                    }
            }
        }

        #endregion

        #region === UI (Basic CUI) ===

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UI_ROOT);
            CuiHelper.DestroyUi(player, UI_BOARD);
            CuiHelper.DestroyUi(player, UI_LICENSES);
        }

        private void UI_ShowBoard(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = _config.UI.PanelColor },
                RectTransform = { AnchorMin = "0.15 0.15", AnchorMax = "0.85 0.85" },
                CursorEnabled = true
            }, "Overlay", UI_ROOT);

            container.Add(new CuiLabel
            {
                Text = { Text = L("UI_Title_Board", player), FontSize = 18, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" }
            }, UI_ROOT);

            container.Add(new CuiButton
            {
                Button = { Color = _config.UI.DangerColor, Command = "hrp.ui close" },
                RectTransform = { AnchorMin = "0.94 0.93", AnchorMax = "0.99 0.99" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, UI_ROOT);

            // list area
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.25" },
                RectTransform = { AnchorMin = "0.03 0.08", AnchorMax = "0.65 0.90" }
            }, UI_ROOT, UI_BOARD);

            // details panel
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.25" },
                RectTransform = { AnchorMin = "0.67 0.08", AnchorMax = "0.97 0.90" }
            }, UI_ROOT, UI_BOARD + "_DETAILS");

            // Populate top N contracts
            var contracts = _store.Contracts.Values
                .OrderByDescending(c => c.CreatedAtUnix)
                .Take(10)
                .ToList();

            float y = 0.86f;
            foreach (var c in contracts)
            {
                string row = $"#{c.Id} | {c.Title} | {c.Reward} | {L("UI_ContractStatus_" + c.Status, player)} | {c.Type}";
                container.Add(new CuiLabel
                {
                    Text = { Text = row, FontSize = 12, Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.03 {y}", AnchorMax = $"0.97 {y + 0.06f}" }
                }, UI_BOARD);

                if (c.Status == ContractStatus.OPEN)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Color = _config.UI.AccentColor, Command = $"hrp.ui board.take {c.Id}" },
                        RectTransform = { AnchorMin = $"0.82 {y}", AnchorMax = $"0.96 {y + 0.05f}" },
                        Text = { Text = L("UI_Take", player), FontSize = 12, Align = TextAnchor.MiddleCenter }
                    }, UI_BOARD);
                }

                y -= 0.07f;
                if (y < 0.08f) break;
            }

            // Details placeholder
            container.Add(new CuiLabel
            {
                Text = { Text = "Select a contract (details panel stub).", FontSize = 12, Align = TextAnchor.UpperLeft },
                RectTransform = { AnchorMin = "0.03 0.03", AnchorMax = "0.97 0.97" }
            }, UI_BOARD + "_DETAILS");

            CuiHelper.AddUi(player, container);
        }

        private void UI_ShowLicenses(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = _config.UI.PanelColor },
                RectTransform = { AnchorMin = "0.25 0.2", AnchorMax = "0.75 0.8" },
                CursorEnabled = true
            }, "Overlay", UI_ROOT);

            container.Add(new CuiLabel
            {
                Text = { Text = L("UI_Title_Licenses", player), FontSize = 18, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = "1 1" }
            }, UI_ROOT);

            container.Add(new CuiButton
            {
                Button = { Color = _config.UI.DangerColor, Command = "hrp.ui close" },
                RectTransform = { AnchorMin = "0.92 0.92", AnchorMax = "0.99 0.99" },
                Text = { Text = "X", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, UI_ROOT);

            var prof = GetOrCreateProfile(player.userID);
            var now = NowUnix();

            var licenseTypes = new[]
            {
                "Trade","Guard","WeaponL1","WeaponL2","WeaponL3","TurretPermit"
            };

            float y = 0.80f;
            foreach (var t in licenseTypes)
            {
                TryGetLicenseDef(t, out var cost, out var days);

                var lic = prof.Licenses.FirstOrDefault(x => x.Type.Equals(t, StringComparison.OrdinalIgnoreCase));
                bool active = lic != null && lic.ExpiresAtUnix > now;

                var status = active ? "ACTIVE" : "INACTIVE";
                var exp = active ? DateTimeOffset.FromUnixTimeSeconds(lic.ExpiresAtUnix).ToString("yyyy-MM-dd HH:mm") : "-";

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{t} | {status} | {exp} | {cost} scrap", FontSize = 12, Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.05 {y}", AnchorMax = $"0.80 {y + 0.06f}" }
                }, UI_ROOT);

                container.Add(new CuiButton
                {
                    Button = { Color = _config.UI.AccentColor, Command = $"hrp.ui licenses.buy {t}" },
                    RectTransform = { AnchorMin = $"0.82 {y}", AnchorMax = $"0.95 {y + 0.05f}" },
                    Text = { Text = active ? L("UI_Renew", player) : L("UI_Buy", player), FontSize = 12, Align = TextAnchor.MiddleCenter }
                }, UI_ROOT);

                y -= 0.08f;
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region === Helpers / Raid Entity Detection ===

        private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private static readonly string[] WeaponLicenseTypes = { "WeaponL1", "WeaponL2", "WeaponL3" };

        private void RecordTransaction(string from, string to, int amount, string reason)
        {
            var tx = new Transaction
            {
                From = from,
                To = to,
                Amount = amount,
                Reason = reason
            };

            _store.Treasury.RecentTransactions.Add(tx);
            if (_store.Treasury.RecentTransactions.Count > 200)
                _store.Treasury.RecentTransactions.RemoveRange(0, _store.Treasury.RecentTransactions.Count - 200);

            AppendTransactionLog(tx);
        }

        private void AppendTransactionLog(Transaction tx)
        {
            if (string.IsNullOrEmpty(_transactionLogPath)) return;
            try
            {
                var line = JsonConvert.SerializeObject(tx);
                File.AppendAllText(_transactionLogPath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to write transaction log: {ex.Message}");
            }
        }

        private void ApplyWeeklyBusinessTax()
        {
            if (!_config.Economy.Enabled) return;

            var tax = Mathf.CeilToInt(_config.Economy.BusinessRegistrationCost * _config.Economy.BusinessTaxRate);
            if (tax <= 0) return;

            var now = NowUnix();
            foreach (var prof in _store.Players.Values)
            {
                if (prof.BusinessUntilUnix <= now) continue;

                if (_economy.TryWithdraw(prof.SteamId, tax))
                {
                    _economy.TreasuryAdd(tax, "business_tax", prof.SteamId.ToString());
                    var player = BasePlayer.FindByID(prof.SteamId);
                    if (player != null && player.IsConnected)
                        Reply(player, "BusinessTaxCharged", tax);
                }
                else
                {
                    var player = BasePlayer.FindByID(prof.SteamId);
                    if (player != null && player.IsConnected)
                        Reply(player, "BusinessTaxFailed", tax);
                }
            }
        }

        private BasePlayer FindOnlinePlayer(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == nameOrId) return p;
            }

            var lower = nameOrId.ToLowerInvariant();
            return BasePlayer.activePlayerList.FirstOrDefault(p => p.displayName.ToLowerInvariant().Contains(lower));
        }

        private PlayerProfile GetOrCreateProfile(ulong steamId)
        {
            if (!_store.Players.TryGetValue(steamId, out var prof))
            {
                prof = new PlayerProfile { SteamId = steamId, LastName = steamId.ToString() };
                _store.Players[steamId] = prof;
            }
            if (prof.Roles == null) prof.Roles = new HashSet<string>();
            if (prof.Licenses == null) prof.Licenses = new List<LicenseEntry>();
            return prof;
        }

        private bool TryGetLicenseDef(string type, out int cost, out int days)
        {
            cost = 0; days = 0;
            var ls = _config.Licenses;

            // Match by known names
            if (type.Equals("Trade", StringComparison.OrdinalIgnoreCase)) { cost = ls.Trade.Cost; days = ls.Trade.DurationDays; return true; }
            if (type.Equals("Guard", StringComparison.OrdinalIgnoreCase)) { cost = ls.Guard.Cost; days = ls.Guard.DurationDays; return true; }
            if (type.Equals("WeaponL1", StringComparison.OrdinalIgnoreCase)) { cost = ls.WeaponL1.Cost; days = ls.WeaponL1.DurationDays; return true; }
            if (type.Equals("WeaponL2", StringComparison.OrdinalIgnoreCase)) { cost = ls.WeaponL2.Cost; days = ls.WeaponL2.DurationDays; return true; }
            if (type.Equals("WeaponL3", StringComparison.OrdinalIgnoreCase)) { cost = ls.WeaponL3.Cost; days = ls.WeaponL3.DurationDays; return true; }
            if (type.Equals("TurretPermit", StringComparison.OrdinalIgnoreCase)) { cost = ls.TurretPermit.Cost; days = ls.TurretPermit.DurationDays; return true; }

            return false;
        }

        private bool HasAnyWeaponLicense(ulong steamId)
        {
            return WeaponLicenseTypes.Any(type => _licenses.HasLicense(steamId, type));
        }

        private string GetWeaponLicenseRequirementText()
        {
            return string.Join("/", WeaponLicenseTypes);
        }

        private bool IsCitySafeWeaponRestricted(BasePlayer player)
        {
            if (player == null || !_config.Zones.Enabled) return false;
            if (_zones.GetZoneType(player.transform.position) != ZoneType.CITY_SAFE) return false;
            var item = player.GetActiveItem();
            if (item == null || !IsWeaponItem(item)) return false;
            return !HasAnyWeaponLicense(player.userID);
        }

        private bool IsRaidRelevantEntity(BaseCombatEntity entity)
        {
            if (entity is BuildingBlock) return true;
            if (entity is Door) return true;

            // optional: cupboards, boxes, etc (usually not raid-damage target)
            // if (entity.ShortPrefabName.Contains("cupboard")) return true;
            return false;
        }

        private static bool IsTurretEntity(BaseEntity entity)
        {
            return entity is AutoTurret || entity is FlameTurret;
        }

        private static bool IsTrapEntity(BaseEntity entity)
        {
            return entity is BearTrap || entity is Landmine || entity is TeslaCoil || entity is GunTrap;
        }

        private static bool IsWeaponItem(Item item)
        {
            return item?.info?.category == ItemCategory.Weapon;
        }

        private static bool IsTradeEntity(BaseEntity entity)
        {
            return entity is ShopFront || entity is VendingMachine;
        }

        private void NotifyOwner(BaseEntity entity, string key)
        {
            if (entity == null || entity.OwnerID == 0) return;
            var owner = BasePlayer.FindByID(entity.OwnerID);
            if (owner != null && owner.IsConnected)
                Reply(owner, key);
        }

        private bool IsRaidDamage(HitInfo info)
        {
            if (info == null) return false;

            // Explosion damage type is a good heuristic
            if (info.damageTypes != null && info.damageTypes.Has(DamageType.Explosion)) return true;

            // Weapon checks (best effort)
            var weapon = info.WeaponPrefab;
            if (weapon != null)
            {
                var name = weapon.ShortPrefabName ?? "";
                if (name.Contains("rocket") || name.Contains("explosive") || name.Contains("c4") || name.Contains("satchel"))
                    return true;
            }

            // You can add melee-as-raid option later
            return false;
        }

        private ulong ResolveOwnerId(BaseEntity entity)
        {
            // Best-effort:
            // 1) OwnerID if present (doors often)
            // 2) Tool Cupboard lookup (not implemented here)
            // 3) return 0 if unknown
            try
            {
                if (entity != null && entity.OwnerID != 0) return entity.OwnerID;
            }
            catch { }
            return 0;
        }

        private static bool HasFlag(ZoneBlockFlags value, ZoneBlockFlags flag) => (value & flag) == flag;

        #endregion

        #region === Minimal Seed Data Commands (Optional Admin) ===

        [ChatCommand("hrp_zone_add")]
        private void CmdZoneAdd(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
            {
                Reply(player, "NoPermission");
                return;
            }
            // Usage: /hrp_zone_add <id> <CITY_SAFE|SUBURB|WILD> <radius>
            if (args.Length < 3)
            {
                player.ChatMessage(L("Prefix", player) + "Usage: /hrp_zone_add <id> <CITY_SAFE|SUBURB|WILD> <radius>");
                return;
            }

            var id = args[0];
            if (!Enum.TryParse(args[1], true, out ZoneType zt))
            {
                player.ChatMessage(L("Prefix", player) + "Invalid zone type.");
                return;
            }
            if (!float.TryParse(args[2], out var radius) || radius <= 0)
            {
                player.ChatMessage(L("Prefix", player) + "Invalid radius.");
                return;
            }

            _store.Zones[id] = new Zone
            {
                Id = id,
                Type = zt,
                Shape = ZoneShape.RADIUS,
                Center = player.transform.position,
                Radius = radius,
                Priority = zt == ZoneType.CITY_SAFE ? 100 : 10
            };

            SaveData();
            player.ChatMessage(L("Prefix", player) + $"Zone '{id}' added at your position with radius {radius}.");
        }

        #endregion

        #region === Quick Demo: Create sample contracts (Optional) ===

        [ChatCommand("hrp_seed_contracts")]
        private void CmdSeedContracts(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
            {
                Reply(player, "NoPermission");
                return;
            }

            // Create a few sample contracts if empty
            if (_store.Contracts.Count == 0)
            {
                _contracts.Create(player.userID, "Deliver cloth", "Bring 1000 cloth to the city warehouse.", 350, ContractType.DELIVERY);
                _contracts.Create(player.userID, "Guard caravan", "Escort caravan to the monument and back.", 700, ContractType.GUARD);
                _contracts.Create(player.userID, "Build shop", "Construct a small shop near the city gate.", 500, ContractType.BUILD);
                SaveData();
                Reply(player, "ContractCreated", _store.NextContractId - 1, 0);
            }

            player.ChatMessage(L("Prefix", player) + "Seeded sample contracts (if empty). Use /board.");
        }

        #endregion
    }
}
