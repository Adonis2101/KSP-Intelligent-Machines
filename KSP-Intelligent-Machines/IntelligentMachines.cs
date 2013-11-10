using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KIM
{

    public class IntelligentMachine : PartModule
    {

        public FXGroup SoundGroup = null;
        String basepath = "IntelligentMachines/parts/KIM/part/sounds/";     //Base path to audio clips.

        Dictionary<String, AudioClip> sounds;       //Dictionary to maintain clips, may not be adding anything past the resource loader (not sure how resource loader works).
        Queue<AudioClip> soundQueue;                //Queue to play sounds. TODO: Split queue by priority, or something, so that alt is ALWAYS top.

        //State storage variables.
        double alt_baro, alt_radio, gee_force;
        bool splashed, landed = true, launched, docked, escaped, orbit;
        bool gearWarn, brakeWarn;

        public IntelligentMachine()
        {
            Debug.Log("KIM: Loading");

            sounds = new Dictionary<string, AudioClip>();
            soundQueue = new Queue<AudioClip>();
        }

        /// <summary>
        /// Start of game engine, load the Intelligent Machines module.
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            //Log start.
            print("KIM: Start");

            //Start super class.
            base.OnStart(state);

            if (state != StartState.Editor)
            {

                //Load audio object.
                SoundGroup.audio = gameObject.AddComponent<AudioSource>();

                playHelper("alert/welcomeback");
            }
        }

        /// <summary>
        /// Update call.
        /// Called each game update cycle.
        /// </summary>
        public override void OnUpdate()
        {
            base.OnUpdate();

            //Fetch current vessel.
            Vessel v = FlightGlobals.ActiveVessel.GetVessel();

            //TODO: allow these calls to be configured.
            //Flight state.
            updateFlightState(v);

            //Altitude calls.
            updateAlt(v);

            //Gee Force calls.
            //updateGee(v);

            updatePlay();

        }

        /// <summary>
        /// Convenience method to add path to audio as required.
        /// </summary>
        /// <param name="sound"></param>
        public void playHelper(String sound)
        {
            //TODO: Queue play events as not to overlap.
            AudioClip ac;

            //If the clip is loaded.
            if (sounds.ContainsKey(sound))
            {
                //Fetch sound.
                sounds.TryGetValue(sound, out ac);
                //Play.
                play(ac);

            }
            //Otherwise, load the sound and play.
            else
            {
                //Generate full path.
                String soundId = basepath + sound;

                //If sound is available.
                if (GameDatabase.Instance.ExistsAudioClip(soundId))
                {
                    //Load sound.
                    ac = GameDatabase.Instance.GetAudioClip(soundId);
                    //Save sound.
                    sounds.Add(sound, ac);
                    //Play.
                    play(ac);

                }
                else
                {
                    Debug.LogError("KIM: Sound file, " + sound + " not found.");
                }
            }
        }

        /// <summary>
        /// PLay method, loads and plays a sound once.
        /// </summary>
        /// <param name="sound"></param>
        public void play(AudioClip sound)
        {
            //Add to play queue.
            soundQueue.Enqueue(sound);
        }

        public void updatePlay()
        {
            //If there is a sound available.
            if (soundQueue.Count > 0)
            {
                //If the audio player is not in use.
                if (!SoundGroup.audio.isPlaying)
                {
                    //Fetch and play the sound.
                    SoundGroup.audio.PlayOneShot(soundQueue.Dequeue());
                }
            }
        }

        //Update vessel flight state.
        private void updateFlightState(Vessel v)
        {
            
            //Launch state.
            if ((v.situation == Vessel.Situations.FLYING) && !launched)
            {
                launched = true;
                landed = false;
            }

            //Escape.
            if ((v.situation == Vessel.Situations.ESCAPING) && !escaped)
            {
                playHelper("alert/escape");
                escaped = true;

            }
            //Reset.
            else if ((v.situation != Vessel.Situations.ESCAPING) && escaped)
            {
                escaped = false;
            }

            //Orbit.
            if ((v.situation == Vessel.Situations.ORBITING) && !orbit)
            {
                playHelper("alert/orbit");
                orbit = true;

            }
            //Reset.
            else if ((v.situation != Vessel.Situations.ORBITING) && orbit)
            {
                orbit = false;
            }

            //Docking.
            if ((v.situation == Vessel.Situations.DOCKED) && !docked) 
            {
                playHelper("alert/dock");
                docked = true;
            }
            //Reset.
            else if ((v.situation != Vessel.Situations.DOCKED) && docked)
            {
                docked = false;
            }

            //Landing and splashdown calls.
            if ((v.situation == Vessel.Situations.SPLASHED) && !splashed)
            {
                playHelper("alert/splash");
                splashed = true;
                launched = false;

                //If we have landed.
            }
            else if ((v.situation == Vessel.Situations.LANDED) && !landed)
            {
                //If we have stopped
                if (v.srf_velocity.sqrMagnitude < 20)
                {
                    playHelper("alert/touch");
                    landed = true;
                    launched = false;
                }
                    //If we are rolling.
                else if(!brakeWarn)
                {
                    //Alert brakes.
                    playHelper("alert/brake");
                    brakeWarn = true;
                }
            }

        }

        // Update control data
        private void updateCont(Vessel v)
        {
            //RCS and SAS state here?
            //Fuel and electricity?
        }

        private void updateMach(Vessel v)
        {
            double pressure = v.staticPressure;
            double density = v.atmDensity;

            //TODO: Work out approximate speed of sound and bogo an algorithm for this.
            
        }

        //Gee force calls.
        private void updateGee(Vessel v)
        {
            //Fetch geeforce.
            double new_gee = Math.Abs(v.geeForce);

            if ((new_gee >= 6) && (gee_force < 6))
            {
                playHelper("gforce/6");
            }
            else if ((new_gee >= 5) && (gee_force < 5))
            {
                playHelper("gforce/5");
            }
            else if ((new_gee >= 4) && (gee_force < 4))
            {
                playHelper("gforce/4");
            }
            else if ((new_gee >= 3) && (gee_force < 3))
            {
                playHelper("gforce/3");
            }
            
            //Update last geforce data.
            gee_force = new_gee;
        }

        //Altitude calls.
        private void updateAlt(Vessel v)
        {
            //Descent calls.
            double height;
            
            double surfHeight = v.GetHeightFromSurface();
            double terrHeight = v.GetHeightFromTerrain();
            double alt = v.altitude;

            //Get smallest of the available altitudes.
            //TODO: work out why this is required.
            height = Math.Min(terrHeight, alt);


            //TODO: invert order to always get correct calls.
            if ((height <= 10) && (alt_radio > 10))
            {
                playHelper("alt/10");
            }
            else if ((height <= 20) && (alt_radio > 20))
            {
                playHelper("alt/20");
            }
            else if ((height <= 30) && (alt_radio > 30))
            {
                playHelper("alt/30");
            }
            else if ((height <= 40) && (alt_radio > 40))
            {
                playHelper("alt/40");
            }
            else if ((height <= 50) && (alt_radio > 50))
            {
                playHelper("alt/50");
            }
            else if ((height <= 100) && (alt_radio > 100))
            {
                playHelper("alt/100");
            }
            else if ((height <= 200) && (alt_radio > 200))
            {
                playHelper("alt/200");
                //TODO: Check gear is down.
            }
            else if ((height <= 300) && (alt_radio > 300))
            {
                playHelper("alt/300");
            }
            else if ((height <= 400) && (alt_radio > 400))
            {
                playHelper("alt/400");
            }
            else if ((height <= 500) && (alt_radio > 500))
            {
                playHelper("alt/500");
            }
            else if ((height <= 1000) && (alt_radio > 1000))
            {
                playHelper("alt/1000");
            }
            else if ((height <= 1500) && (alt_radio > 1500))
            {
                playHelper("alt/1500");
            }
            else if ((height <= 2000) && (alt_radio > 2000))
            {
                playHelper("alt/2000");
            }
            else if ((height <= 3000) && (alt_radio > 3000))
            {
                playHelper("alt/radioalt");
            }

            //Update radio altitude.
            alt_radio = height;
        }


    }
}
