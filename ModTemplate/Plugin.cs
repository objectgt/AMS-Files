using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ARS
{
    [BepInIncompatibility("industry.resurgence")]
    [BepInPlugin("com.industry.autoreportsys", "Automatic Reporting System", "1.0.0")]
    internal class ARS : BaseUnityPlugin
    {
        #region Main

        void Start()
        {
            this.AddComponent<PhotonCallbacks>();

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

        public static void CheckServer()
        {
            if (PlayersToReport.Length == 0) return;

            if (PhotonNetwork.InRoom)
                foreach (Player plr in PhotonNetwork.PlayerListOthers)
                {
                    if (PlayersChecked.Contains(plr)) continue;

                    if (NeedToReport(plr))
                    {
                        GorillaPlayerScoreboardLine.ReportPlayer(plr.UserId,
                            GorillaPlayerLineButton.ButtonType.Toxicity, plr.NickName);
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

        public static void CheckUser(Player plrToCheck)
        {
            if (!PlayersChecked.Contains(plrToCheck) && NeedToReport(plrToCheck))
            {
                GorillaPlayerScoreboardLine.ReportPlayer(plrToCheck.UserId,
                    GorillaPlayerLineButton.ButtonType.Toxicity, plrToCheck.NickName);

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

    public class PhotonCallbacks : MonoBehaviourPunCallbacks
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
            ARS.CheckServer();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);

            try
            {
                ARS.CheckUser(newPlayer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #endregion
    }
}
