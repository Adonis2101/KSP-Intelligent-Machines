using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Speech.Recognition;

namespace KSPIntelligentMachines
{
    class VoiceController
    {
        SpeechRecognizer sr;

        public VoiceController(String[] commandStrings, EventHandler<SpeechRecognizedEventArgs> recognized)
        {
            //Instantiate speech recogniser.
            sr = new SpeechRecognizer();

            //Create grammar
            Choices commands = new Choices();
            commands.Add(commandStrings.ToArray());
            GrammarBuilder gb = new GrammarBuilder(commands);
            Grammar g = new Grammar(gb);

            //Load grammar to speech object.
            sr.LoadGrammar(g);

            //Attach recognition handler.
            sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognized);
            //Attach failure handler to suppress pop-up on failure.
            sr.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(commandFailed);

        }

        void commandRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            System.Console.WriteLine("Recognized: " + e.Result.Text);
        }

        /// <summary>
        /// Command failed handler.
        /// Used to suppress pop up box for failed recognition.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void commandFailed(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            System.Console.WriteLine("Recognition failed");
        }

    }
}
