using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using CortanaSocialEvents.Resources;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Windows.Foundation;
using Windows.Phone.Speech.Recognition;
using Windows.Phone.Speech.Synthesis;
using Windows.Phone.Speech.VoiceCommands;
using System.Net.Http;
using Microsoft.Phone.Tasks;


namespace CortanaSocialEvents
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            InitNoiseWords();
            InstallVoiceCommands();
            InitializeRecognizer();
            this.Synthesizer = new SpeechSynthesizer();
        }

        // State maintenance of the Speech Recognizer
        private SpeechRecognizer Recognizer;
        private AsyncOperationCompletedHandler<SpeechRecognitionResult> recoCompletedAction;
        private IAsyncOperation<SpeechRecognitionResult> CurrentRecognizerOperation;

        // State maintenance of the Speech Synthesizer
        private SpeechSynthesizer Synthesizer;
        private IAsyncAction CurrentSynthesizerAction;

        private List<string> noiseWords = new List<string>();

        private void InitNoiseWords()
        {
            noiseWords.Add("cortana");
            noiseWords.Add("find");
            noiseWords.Add("me");
            noiseWords.Add("events");
            noiseWords.Add("containing");
            noiseWords.Add("the");
            noiseWords.Add("event");
            noiseWords.Add("for");
        }

        private void SpeechActionButtonContainer_Tap(object sender, GestureEventArgs e)
        {
            StartListening();
        }

        private void StartListening()
        {
            try
            {
                // Start listening to the user and set up the completion handler for when the result
                //*******************************************************************************************************
                //this fires when you press the button to start listening 
                //*******************************************************************************************************
                this.CurrentRecognizerOperation = this.Recognizer.RecognizeAsync();
                this.CurrentRecognizerOperation.Completed = recoCompletedAction;

                PlaySound("Assets/ListeningEarcon.wav");
                SearchTextBox.Text = "Listening...";

                EventsLst.Visibility = System.Windows.Visibility.Collapsed;
                WaitBorder.Visibility = System.Windows.Visibility.Visible;
                WaitImg.Visibility = System.Windows.Visibility.Visible;
                WaitTxt.Visibility = System.Windows.Visibility.Visible;
                WaitTxt.Text = "I await your command...";
            }
            catch (Exception recoException)
            {
                PlaySound("Assets/CancelledEarcon.wav");

                MessageBox.Show("Sorry, there was a problem with the voice system:  " +
                    recoException.Message);

                recoCompletedAction.Invoke(null, AsyncStatus.Error);
            }
        }

        private void StartSpeakingSsml(string ssmlToSpeak)
        {
            // Begin speaking using our synthesizer, wiring the completion event to stop tracking the action when it
            // finishes.
            //*******************************************************************************************************
            //this fires after you have spoken what you are searching for, and after it's done it repeats back to 
            //you what you are searching for, i.e. "searching for SharePoint"
            //*******************************************************************************************************
            this.CurrentSynthesizerAction = this.Synthesizer.SpeakSsmlAsync(ssmlToSpeak);
            this.CurrentSynthesizerAction.Completed = new AsyncActionCompletedHandler(
                (operation, asyncStatus) =>
                {
                    Dispatcher.BeginInvoke(() => { this.CurrentSynthesizerAction = null; });
                });
        }

        private async void InitializeRecognizer()
        {
            this.Recognizer = new SpeechRecognizer();
            this.Recognizer.Grammars.AddGrammarFromPredefinedType("search", SpeechPredefinedGrammar.WebSearch);

            //note used, but here's an example of the other predefined grammer collections:
            //this.Recognizer.Grammars.AddGrammarFromPredefinedType("dictation", SpeechPredefinedGrammar.Dictation);

            await this.Recognizer.PreloadGrammarsAsync();

            recoCompletedAction = new AsyncOperationCompletedHandler<SpeechRecognitionResult>((operation, asyncStatus) =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    this.CurrentRecognizerOperation = null;

                    switch (asyncStatus)
                    {
                        case AsyncStatus.Completed:
                            SpeechRecognitionResult result = operation.GetResults();
                            if (!String.IsNullOrEmpty(result.Text))
                            {                                
                                //execute my search here!
                                LaunchSearch(result.Text);
                            }
                            break;
                        case AsyncStatus.Error:
                            if (operation == null)
                                MessageBox.Show("Sorry, you haven't accepted the privacy policy for voice recognition yet " +
                                    "so I can't help you.");
                            else
                                MessageBox.Show("Sorry, there was a problem recognizing your search request: " + 
                                    operation.ErrorCode.Message);
                            break;
                        default:
                            break;
                    }
                });
            });
        }

        private void LaunchSearch(string queryTerms)
        {
            //we really only want to look for a single phrase, which could be several words
            //but in a tag are repreresented as a single word
            //in order to do that we'll take all the words we're given, 
            //extract out the noise words, and then create a single word
            //that's a concatenation of all the remaining non-words into a single phrase
            string searchTerm = GetSearchTerm(queryTerms);

            try
            {
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    //create the template for speaking back to us
                    string htmlEncodedQuery = HttpUtility.HtmlEncode(searchTerm);
                    StartSpeakingSsml(String.Format(AppResources.SpokenSearchShortTemplate, htmlEncodedQuery));

                    //update UI
                    Dispatcher.BeginInvoke(() =>
                    {
                        SearchTextBox.Text = searchTerm;
                        WaitBorder.Visibility = System.Windows.Visibility.Visible;
                        WaitImg.Visibility = System.Windows.Visibility.Visible;
                        WaitTxt.Visibility = System.Windows.Visibility.Visible;
                        WaitTxt.Text = "Searching for \"" + searchTerm + "\" events...";
                    });

                    //execute the query
                    QueryEvents(searchTerm);
                }
                else
                {
                    WaitBorder.Visibility = System.Windows.Visibility.Visible;
                    WaitImg.Visibility = System.Windows.Visibility.Visible;
                    WaitTxt.Visibility = System.Windows.Visibility.Visible;
                    WaitTxt.Text = "Sorry, there were only noise words in your search request";
                }
            }
            catch
            {
                WaitBorder.Visibility = System.Windows.Visibility.Visible;
                WaitImg.Visibility = System.Windows.Visibility.Visible;
                WaitTxt.Visibility = System.Windows.Visibility.Visible;
                WaitTxt.Text = "Sorry, there was a problem finding your event";
            }
        }

        private string GetSearchTerm(string value)
        {
            string result = value;

            try
            {
                //create an array of words
                string[] words = value.Split(" ".ToCharArray());

                //create what our new word will be
                System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);

                //compare and add if not noise word
                foreach(string word in words)
                {
                    if (!noiseWords.Contains(word.ToLower()))
                        sb.Append(word);
                }

                //write out the results
                result = sb.ToString();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine("Error: " + ex.Message);
            }

            return result;
        }

        private async void QueryEvents(string searchTerms)
        {
            //do an async query to our REST endpoint

            string searchUrl = "https://socialevents.azurewebsites.net/api/events/search?tagName=" + searchTerms;

            HttpClient hc = new HttpClient();
            string data = await hc.GetStringAsync(searchUrl);

            //if we got some data back then try and load it into our set of search results of 
            //social events and Yammer messages
            if (!string.IsNullOrEmpty(data))
            {
                CortanaSearchResult csr = CortanaSearchResult.GetInstanceFromJson(data);

                Dispatcher.BeginInvoke(() =>
                {
                    //if we have some data then plug into our UI
                    if ((csr != null) && ((csr.Events.Count > 0) || (csr.YammerMessages.Count > 0)))
                    {
                        WaitBorder.Visibility = System.Windows.Visibility.Collapsed;
                        WaitImg.Visibility = System.Windows.Visibility.Collapsed;
                        WaitTxt.Visibility = System.Windows.Visibility.Collapsed;
                        EventsLst.Visibility = System.Windows.Visibility.Visible;

                        EventsLst.DataContext = csr.Events;
                        YammerLst.DataContext = csr.YammerMessages;
                    }
                    else
                    {
                        //update the UI to show that there were no search results found
                        WaitBorder.Visibility = System.Windows.Visibility.Visible;
                        WaitImg.Visibility = System.Windows.Visibility.Visible;
                        WaitTxt.Visibility = System.Windows.Visibility.Visible;

                        string notFoundMessage = "Sorry, I couldn't find any results for \"" +
                            searchTerms + "\"";

                        WaitTxt.Text = notFoundMessage;

                        string spokenMessage = HttpUtility.HtmlEncode(notFoundMessage);
                        StartSpeakingSsml(String.Format(AppResources.SpokenShortSearchEmptyTemplate, searchTerms));
                    }

                    SearchTextBox.Text = "";
                });
            }
        }

        private void PlaySound(string path)
        {
            //PlaySound("Assets/CancelledEarcon.wav");
            //PlaySound("Assets/ListeningEarcon.wav");

            var stream = TitleContainer.OpenStream(path);
            var effect = SoundEffect.FromStream(stream);
            FrameworkDispatcher.Update();
            effect.Play();
        }

        private void EventNameTxt_Tap(object sender, GestureEventArgs e)
        {
            string url = ((FrameworkElement)sender).Tag as string;

            if (!string.IsNullOrEmpty(url))
            {
                WebBrowserTask bt = new WebBrowserTask();
                bt.Uri = new Uri(url, UriKind.Absolute);
                bt.Show();
            }
        }

        private async void InstallVoiceCommands()
        {
            const string wp80vcdPath = "ms-appx:///VoiceCommandDefinition_8.0.xml";
            const string wp81vcdPath = "ms-appx:///VoiceCommandDefinition_8.1.xml";

            try
            {
                bool using81orAbove = ((Environment.OSVersion.Version.Major >= 8)
                    && (Environment.OSVersion.Version.Minor >= 10));

                Uri vcdUri = new Uri(using81orAbove ? wp81vcdPath : wp80vcdPath);

                await VoiceCommandService.InstallCommandSetsFromFileAsync(vcdUri);
            }
            catch (Exception vcdEx)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Sorry, there was a problem installing the voice commands for Cortana, you'll only be " + 
                        "able to use voice commands within this app.  The error was: " + vcdEx.Message);
                });
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Is this a new activation or a resurrection from tombstone?
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New)
            {

                //look to see if it was launched using search terms
                if (NavigationContext.QueryString.ContainsKey("dictatedSearchTerms"))
                {

                    //if so get the search terms and execute a query
                    string queryTerms = NavigationContext.QueryString["dictatedSearchTerms"];
                    LaunchSearch(queryTerms);
                }
            }
        }
    }

    public class YammerDateConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object ret = value;

            try
            {
                string oldDate = (string)ret;

                //look for the plus sign
                int i = oldDate.IndexOf("+");

                if (i > -1)
                    oldDate = oldDate.Substring(0, i - 2);

                DateTime newDate = DateTime.Parse(oldDate);
                oldDate = newDate.ToShortDateString() + " " + newDate.ToShortTimeString();

                ret = oldDate;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //throw new NotImplementedException();
            //should never have to do this, won't implement it
            return value;
        }
    }

    public class YammerExcerptConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object ret = value;

            try
            {
                string oldValue = (string)ret;

                //shorten it to max chars
                const int MAX_CHARS = 100;

                if (oldValue.Length > MAX_CHARS)
                    oldValue = oldValue.Substring(0, MAX_CHARS - 1) + "...";

                ret = oldValue;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //throw new NotImplementedException();
            //should never have to do this, won't implement it
            return value;
        }
    }

}