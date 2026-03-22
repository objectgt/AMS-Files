namespace ARS
{
    internal class ARS : MonoBehaviourPunCallbacks
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
            yield return new WaitForSeconds(2.5f);
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
            if (!Directory.Exists("ARS"))
                Directory.CreateDirectory("ARS");

            _ = AsyncGetPlayerIDs();

            EasierLog("ARS fully initialized, thank you for helping the gorilla tag modding community!");
        }

        public static List<Player> PlayersChecked = new List<Player>();
        public static string PlayerIDs = string.Empty;
        public static string[] PlayersToReport = new string[0];
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
            if (PlayersToReport.Length == 0) return;

            if (PhotonNetwork.InRoom)
                foreach (Player plr in PhotonNetwork.PlayerListOthers)
                {
                    if (PlayersChecked.Contains(plr)) continue;

                    if (NeedToReport(plr))
                    {
                        foreach (GorillaPlayerScoreboardLine scoreboardLine in
                                 GorillaScoreboardTotalUpdater.allScoreboardLines.Where(scoreboardLine =>
                                     scoreboardLine.linePlayer ==
                                     NetworkSystem.Instance.GetNetPlayerByID(plr.ActorNumber)))
                        {
                            scoreboardLine.reportedToxicity = true;
                            scoreboardLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);
                        }
                        EasierLog($"Reported user {plr.NickName}.");
                    }

                    PlayersChecked.Add(plr);
                }

            if (!HasChecked && PhotonNetwork.InRoom)
            {
                LastRoomChecked = PhotonNetwork.CurrentRoom.Name;
                HasChecked = true;
            }
        }

        static void CheckUser(Player plrToCheck)
        {
            if (!PlayersChecked.Contains(plrToCheck) && NeedToReport(plrToCheck))
            {
                foreach (GorillaPlayerScoreboardLine scoreboardLine in
                         GorillaScoreboardTotalUpdater.allScoreboardLines.Where(scoreboardLine =>
                             scoreboardLine.linePlayer ==
                             NetworkSystem.Instance.GetNetPlayerByID(plrToCheck.ActorNumber)))
                {
                    scoreboardLine.reportedToxicity = true;
                    scoreboardLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);
                }

                EasierLog($"Reported user {plrToCheck.NickName}.");
                PlayersChecked.Add(plrToCheck);
            }
        }

        [Obsolete("Use async version instead. This blocks main thread.")]
        static void GetPlayerIDs()
        {
            if (PlayerIDs == string.Empty)
                PlayerIDs = new WebClient()
                    .DownloadString(
                        "https://raw.githubusercontent.com/AutoReportSystem/ARSPlayerIDs/refs/heads/main/Player%20Ids.txt")
                    .Trim();

            PlayersToReport = PlayerIDs.Split(',');

            EasierLog($"Recieved player ids to report. Count of users: {PlayersToReport.Count()}");
        }

        private static readonly HttpClient Client = new HttpClient();

        static async Task AsyncGetPlayerIDs()
        {
            PlayerIDs = await Client.GetStringAsync(
                "https://raw.githubusercontent.com/AutoReportSystem/ARSPlayerIDs/refs/heads/main/Player%20Ids.txt");
            PlayerIDs = PlayerIDs.Trim();

            PlayersToReport = PlayerIDs.Split(',');

            EasierLog($"Recieved player ids to report. Count of users: {PlayersToReport.Count()}");
        }

        public static bool NeedToReport(Player plr)
        {
            if (plr == null)
                return false;

            for (int i = 0; i < PlayersToReport.Length; i++)
                if (PlayersToReport[i] == plr.UserId)
                    return true;

            return false;
        }

        static void EasierLog(string message)
        {
            Console.WriteLine($"[ARS LOGGING] {message}");
        }

        #endregion
}
