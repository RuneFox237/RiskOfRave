using BepInEx;
using BepInEx.Configuration;
using RoR2;
//using R2API;
//using R2API.Utils;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices; //marshall
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using On.RoR2;
using On.RoR2.UI;
using RoR2.UI;
//using static R2API.SoundAPI;
using System.Reflection;


/* TODO:
 * [X]VOLUME
 * [ ] Add volume slider in-game using 'Risk Of Options'
 * [ ]Add More Songs
 * [ ]Add slight screen bounce zoom on beat
 * [ ]Add Rave Lasers to Teleporter
 * [ ]Remove OG music while playing
 * [ ]Add Support for songs in Moisture Upset
*/


namespace RuneFoxMods
{
//  [BepInDependency("com.bepis.r2api")]
  //Change these
  [BepInPlugin("com.RuneFoxMods.RiskOfRave", "RiskOfRave", "1.0.3")]
//  [R2APISubmoduleDependency("SoundAPI")]
  public class RiskOfRave : BaseUnityPlugin
  {
    public class Conductor
    {
      public float bpm = 165; //bpm of the song
      public float crochet = 60f/165f; //time duration of a beat. calculated from bpm
      public float offset = 0.4f; //mp3s usually have a tiny gap at beginning, this is to help w/ that
      public float songposition = 0; //position of song in dspTime, updates every frame
      float dspTimeSongStart = 0; //the dspTime that the song started at;
      public bool isPlaying = false;

      public void updateConductor()
      {
        //Udpate this for wwise
        if (isPlaying)
        {
          //  songposition = (float)(AudioSettings.dspTime - dspTimeSongStart) /* * song.pitch*/ - offset;
          songposition = (float)(Time.time - dspTimeSongStart) /* * song.pitch*/ - offset;
        }
      }

      public void startConductor()
      {
        //dspTimeSongStart = (float)AudioSettings.dspTime;
        dspTimeSongStart = Time.time;
        isPlaying = true;
      }

      public void stopConductor()
      {
        isPlaying = false;
      }
    }

    public static ConfigEntry<int> Volume { get; set; }


    Conductor conductor = new Conductor();
    RoR2.MusicController MusicCon = null;
    RoR2.HoldoutZoneController Hodl;
    RoR2.UI.ObjectivePanelController.ObjectiveTracker HodlTracker;

    /////////////////////////////////////////
    //Custom version
    byte[] bytes;
    //Custom version
    /////////////////////////////////////////

    uint BankID; //used for the non-R2API version
    public void Awake()
    {
      //      Debug.Log("TestModLoaded");
      Volume = Config.Bind<int>("Config", "Volume", 100, "How loud the music will be on a scale from 0-100");

      //load the rave music into sound banks
      using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("RiskOfRave.Rave.bnk"))
      {
        //////////////////////////////////////////
        //R2API version
        //var bytes = new byte[bankStream.Length];
        //bankStream.Read(bytes, 0, bytes.Length);
        //SoundBanks.Add(bytes);
        //R2API version
        //////////////////////////////////////////


        /////////////////////////////////////////
        //Custom version
        bytes = new byte[bankStream.Length];
        bankStream.Read(bytes, 0, bytes.Length);
        //Ccustom version
        /////////////////////////////////////////
      }

      //      Debug.Log("TestModLoaded2");



      //On.RoR2.HoldoutZoneController.Start += StartRave;
      //On.RoR2.HoldoutZoneController.FullyChargeHoldoutZone += EndRaveChargedHoldout;
      On.RoR2.HoldoutZoneController.OnEnable += StartRaveTest;
      On.RoR2.HoldoutZoneController.OnDisable += EndRaveTest;
      //On.RoR2.UI.ObjectivePanelController.FinishTeleporterObjectiveTracker.ctor += EndRaveChargedTele;
      On.RoR2.GameOverController.SetRunReport += EndRaveDeath;
      
      /////////////////////////////////////////
      //Custom version
      On.RoR2.RoR2Application.OnLoad += OnLoad;
      //Custom version
      /////////////////////////////////////////


      On.RoR2.UI.HUD.Awake += RaveUI;
      //On.RoR2.TeleporterInteraction.OnInteractionBegin += StartConductor;


      On.RoR2.UI.ObjectivePanelController.AddObjectiveTracker += (orig, self, tracker) =>
      {
        orig(self, tracker);
        if (tracker.ToString() == "RoR2.HoldoutZoneController+ChargeHoldoutZoneObjectiveTracker")
        {
          HodlTracker = tracker;
        }

//        Debug.Log("===================================================");
//        Debug.Log("Objective Tracker Added: " + tracker.ToString());
//        Debug.Log("===================================================");
      };

      On.RoR2.UI.ObjectivePanelController.RemoveObjectiveTracker += (orig, self, tracker) =>
      {
        orig(self, tracker);
        if (tracker.ToString() == "RoR2.HoldoutZoneController+ChargeHoldoutZoneObjectiveTracker")
        {
          HodlTracker = null;
        }

//        Debug.Log("===================================================");
//        Debug.Log("Objective Tracker Removed: " + tracker.ToString());
//        Debug.Log("===================================================");
      };

      //TODO: create a prefab of a image that is scaled across the entire screen and load it in
    }


    /////////////////////////////////////////
    //Custom version
    private void OnLoad(On.RoR2.RoR2Application.orig_OnLoad orig, RoR2.RoR2Application self)
    {
      orig(self);

      //Creates IntPtr of sufficient size.
      IntPtr Memory = Marshal.AllocHGlobal(bytes.Length);
      //copies the byte array to the IntPtr
      Marshal.Copy(bytes, 0, Memory, bytes.Length);

      //Loads the entire IntPtr as a bank
      var result = AkSoundEngine.LoadBank(Memory, (uint)bytes.Length, out BankID);
      if (result != AKRESULT.AK_Success)
      {
        Debug.LogError("Risk of Rave SoundBank failed to to load with result " + result);
      }
    }
    //Custom version
    /////////////////////////////////////////


    private void EndRaveTest(On.RoR2.HoldoutZoneController.orig_OnDisable orig, RoR2.HoldoutZoneController self)
    {
      EndRave();
      orig(self);

      Hodl = null;
    }



    GameObject RaveTint;
    RectTransform RaveTintRect;
    Image RaveTintImg;
    private void RaveUI(On.RoR2.UI.HUD.orig_Awake orig, RoR2.UI.HUD self)
    {
      orig(self);

      RaveTint = new GameObject();
      RaveTint.name = "RaveTint";
      RaveTintRect = RaveTint.AddComponent<RectTransform>();
      RaveTintRect.parent = self.mainContainer.transform;

      //var test1 = self.mainUIPanel.GetComponent<RectTransform>();
      //var test2 = self.mainContainer.GetComponent<RectTransform>();
      //var test3 = self.transform.GetComponent<RectTransform>();
      //if (test1)
      //{
      //  Debug.Log("MainPannel height: " + test1.rect.height + "  width: " + test1.rect.width);
      //}
      //if (test2)
      //{
      //  Debug.Log("Maincontainter height: " + test2.rect.height + "  width: " + test2.rect.width);
      //}
      //if (test3)
      //{
      //  Debug.Log("Maincontainter height: " + test3.rect.height + "  width: " + test3.rect.width);
      //}
      //Screen.width;

      RaveTintRect.anchorMax = Vector2.one;
      RaveTintRect.anchorMin = Vector2.zero;
      //RaveTintRect..width = Screen.width;
      RaveTintRect.localScale = new Vector3(10, 10, 10);
      RaveTintRect.anchoredPosition = Vector2.zero;
      RaveTintImg = RaveTint.AddComponent<Image>();
      RaveTintImg.color = new Color(1, 1, 1, 0f);
      RaveTintImg.raycastTarget = false;
    }

    public float lastBeat = 0;
    
    float lastHue = 0;
    float alpha = 0.1f;
    public void Update()
    {
      if(conductor != null)
      {
        conductor.updateConductor();

        if (conductor.songposition >= lastBeat + conductor.crochet && conductor.isPlaying)
        {
          //do the thing every beat
          lastHue += UnityEngine.Random.Range(0.25f, 0.75f);
          lastHue = Mathf.Repeat(lastHue, 1f);
          //Debug.Log(lastHue);
          Color newColor = Color.HSVToRGB(lastHue, 1f, 0.5f);
          //Color newColor = UnityEngine.Random.ColorHSV(0, 1, 0.75f, 1, 0.45f, 0.75f);

          //hardcode the start of the tint to start on the first base
          if(RaveTint != null && conductor.songposition >= 1.1f)
            RaveTintImg.color = new Color(newColor.a, newColor.g, newColor.b, alpha);

          //increment lastbeat
          lastBeat = lastBeat + conductor.crochet;
        }
        else if (conductor.isPlaying == false)
        {
          //having some issue clearing the color, this should fix it
          if (RaveTint != null)
            RaveTintImg.color = new Color(1, 1, 1, 0);
        }
      }

      //Should be able to get the music controller since it's in Don't destroy on load?
      if(MusicCon == null)
      {
        var con = GameObject.FindObjectOfType<RoR2.MusicController>();
        if (con)
          MusicCon = con;

//        Debug.Log("MusicCOntroller: " + MusicCon);
      }

      if (HodlTracker != null && Hodl)
      {
        var local = RoR2.LocalUserManager.GetFirstLocalUser();
        bool charging = Hodl.IsBodyInChargingRadius(local.cachedBody);
        if (charging)
        {
          var test = AkSoundEngine.SetRTPCValue("inNOut", 0);
//          Debug.Log("code: " + test);
        }
        else
        {
          var test = AkSoundEngine.SetRTPCValue("inNOut", 1);
//          Debug.Log("code: " + test);
        }
        
        //set the volume here too
        AkSoundEngine.SetRTPCValue("RaveVolume", Volume.Value);
      }
    }

    private void StartRave(On.RoR2.HoldoutZoneController.orig_Start orig, RoR2.HoldoutZoneController self)
    {
      orig(self);
//      Debug.Log("Start Conductor");
      //spawn the rave prefabs
    
      //Play music
      if (MusicCon)
      {
//        Debug.Log("Try play");
        //uint test = RoR2.Util.PlaySound("RaveStart", MusicCon.gameObject);
        uint test = AkSoundEngine.PostEvent("RaveStart", MusicCon.gameObject);
        //        Debug.Log("code: " + test);
      }
      conductor.startConductor();
    }

    private void StartRaveTest(On.RoR2.HoldoutZoneController.orig_OnEnable orig, RoR2.HoldoutZoneController self)
    {
      orig(self);

      //      Debug.Log("Start Conductor");
      //spawn the rave prefabs
      Hodl = self;

      //Play music
      if (MusicCon)
      {
        //        Debug.Log("Try play");
        //uint test = RoR2.Util.PlaySound("RaveStart", MusicCon.gameObject);
        uint test = AkSoundEngine.PostEvent("RaveStart", MusicCon.gameObject);
        //        Debug.Log("code: " + test);
      }
      conductor.startConductor();
    }

    private void EndRaveChargedHoldout(On.RoR2.HoldoutZoneController.orig_FullyChargeHoldoutZone orig, RoR2.HoldoutZoneController self)
    {
      EndRave();
      orig(self);
    }

    private void EndRaveChargedTele(On.RoR2.UI.ObjectivePanelController.FinishTeleporterObjectiveTracker.orig_ctor orig, RoR2.UI.ObjectivePanelController.ObjectiveTracker self)
    //private void EndRaveCharged(On.RoR2.HoldoutZoneController.orig_FullyChargeHoldoutZone orig, RoR2.HoldoutZoneController self)
    {
      EndRave();
      orig(self);
    }

    private void EndRaveDeath(On.RoR2.GameOverController.orig_SetRunReport orig, RoR2.GameOverController self, RoR2.RunReport newRunReport)
    {
      EndRave();
      orig(self, newRunReport);
    }

    void EndRave()
    {
      Hodl = null;
      //Play music
      if (MusicCon)
      {
        Debug.Log("Try Stop");
        //uint test = RoR2.Util.PlaySound("RaveStop", MusicCon.gameObject);
        uint test = AkSoundEngine.PostEvent("RaveStop", MusicCon.gameObject);
        Debug.Log("code: " + test);
      }
      RaveTintImg.color = new Color(1, 1, 1, 0);
      Debug.Log("End Conductor");
      conductor.stopConductor();
      lastBeat = 0;
    }


  }
}