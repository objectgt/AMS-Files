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

namespace AMS;

[BepInIncompatibility("industry.resurgencev2")]
[BepInPlugin("com.industry.objectgt.automutesys", "Automatic Muting System", "1.0.0")]
internal class AMS : BaseUnityPlugin
{
    #region Main

    void Start()
    {
        this.AddComponent<PhotonCallbacks>();

        if (!Directory.Exists("AMS"))
            Directory.CreateDirectory("AMS");

        _ = AsyncGetPlayerIDs();

        EasierLog("AMS fully initialized, thank you for helping the gorilla tag anti-toxicity community!");
    }

    public static List<Player> PlayersChecked = new();
    public static string PlayerIDs = string.Empty;
    public static HashSet<string> PlayersToMute = new();
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

    public static void CheckUser(Player plrToCheck)
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

    private static readonly HttpClient Client = new();

    static async Task AsyncGetPlayerIDs()
    {
        PlayerIDs = await Client.GetStringAsync(
            "https://www.objectgt.org/api/serverdata/otherserverdata");
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
        AMS.CheckServer();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        try
        {
            AMS.CheckUser(newPlayer);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    #endregion
}
