namespace AMS
{
    internal class AMS : MonoBehaviourPunCallbacks
    {
        #region PhotonOverrides

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            try
            {
                StartCoroutine(DelayedCheckServer());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        IEnumerator DelayedCheckServer()
        {
            yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f, 10f));
            CheckServer();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            try
            {
                CheckUser(newPlayer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion

        #region Main

        void Start()
        {
            if (!Directory.Exists("AMS"))
                Directory.CreateDirectory("AMS");

            _ = AsyncGetPlayerIDs();

            EasierLog("AMS fully initialized, thank you for helping the gorilla tag peace & anti-toxicity community!");
        }

        public static List<Player> PlayersChecked = new List<Player>();
        public static string PlayerIDs = string.Empty;
        public static HashSet<string> PlayersToMute = new();
        static bool HasChecked = false;

        static string LastRoomChecked = string.Empty;

        void Update()
        {

            if (PhotonNetwork.InRoom)
                if (HasChecked && PhotonNetwork.CurrentRoom.Name != LastRoomChecked)
                {
                    HasChecked = false;
                    PlayersChecked.Clear();
                }
        }

        static void CheckServer()
        {
            if (PlayersToMute.Count == 0) return;

            if (PhotonNetwork.InRoom)
                foreach (Player plr in PhotonNetwork.PlayerListOthers)
                    CheckUser(plr);

            if (!HasChecked && PhotonNetwork.InRoom)
            {
                LastRoomChecked = PhotonNetwork.CurrentRoom.Name;
                HasChecked = true;
            }
        }

        static void CheckUser(Player plrToCheck)
        {
            if (!PlayersChecked.Contains(plrToCheck) && NeedToMute(plrToCheck))
            {
                foreach (GorillaPlayerScoreboardLine scoreboardLine in
                         GorillaScoreboardTotalUpdater.allScoreboardLines.Where(scoreboardLine =>
                             scoreboardLine.linePlayer ==
                             NetworkSystem.Instance.GetNetPlayerByID(plrToCheck.ActorNumber)))
                {
                    scoreboardLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Mute);
                }

                EasierLog($"Muted user {plrToCheck.NickName}.");
                PlayersChecked.Add(plrToCheck);
            }
        }

        private static readonly HttpClient Client = new HttpClient();

        static async Task AsyncGetPlayerIDs()
        {
            PlayerIDs = await Client.GetStringAsync(
                "https://www.objectgt.org/api/serverdata/AMS");
            PlayerIDs = PlayerIDs.Trim();

            PlayersToMute = PlayerIDs.Split(",").Select(id => id.Trim())
                .Where(id => !id.IsNullOrEmpty()).ToHashSet();

            EasierLog($"Recieved player ids to mute. Count of users: {PlayersToMute.Count()}");
        }

        public static bool NeedToMute(Player plr)
        {
            if (plr == null)
                return false;

            return PlayersToMute.Contains(plr.UserId);
        }

        static void EasierLog(string message)
        {
            Console.WriteLine($"[AMS LOGGING] {message}");
        }

        #endregion
    }
}
