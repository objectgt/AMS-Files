using BepInEx;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace ARS;

[BepInIncompatibility("industry.resurgencev2")]
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

    public static List<Player> PlayersChecked = new();
    public static string PlayerIDs = string.Empty;
    public static HashSet<string> PlayersToReport = new();
    static bool HasChecked;

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
        if (PlayersToReport.Count == 0) return;

        if (PhotonNetwork.InRoom)
            foreach (Player plr in PhotonNetwork.PlayerListOthers)
                CheckUser(plr);

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

    private static readonly HttpClient Client = new();

    static async Task AsyncGetPlayerIDs()
    {
        PlayerIDs = await Client.GetStringAsync(
            "https://raw.githubusercontent.com/AutoReportSystem/ARSPlayerIDs/refs/heads/main/Player%20Ids.txt");
        PlayerIDs = PlayerIDs.Trim();

        PlayersToReport = PlayerIDs.Split(",").Select(id => id.Trim())
            .Where(id => !id.IsNullOrEmpty()).ToHashSet();

        EasierLog($"Recieved player ids to report. Count of users: {PlayersToReport.Count()}");
    }

    public static bool NeedToReport(Player plr)
    {
        if (plr == null)
            return false;

        return PlayersToReport.Contains(plr.UserId);
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
        yield return new WaitForSeconds(UnityEngine.Random.Range(2.5f, 10f));
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