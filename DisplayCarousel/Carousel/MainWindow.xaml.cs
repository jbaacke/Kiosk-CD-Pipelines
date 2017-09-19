using Carousel.Display_Styles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Timer = System.Timers.Timer;

namespace Carousel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Members of MainWindow


        

        // I will have a timer to move between pages. 
        private static System.Timers.Timer PageTimer = new System.Timers.Timer();

        // This timer will handle how long it has been since the user interacted with the screen.
        private static System.Timers.Timer PreTrackSessionTimer = new System.Timers.Timer();

        // This is the timout for no interaction with the screen on an active session triggering a logout and reset
        private double PreTrackSessionTimeout = 10;

        private bool TrackSession = false;
        private bool PreTrackSession = false;

        // This timer will handle how long it has been since the user interacted with the screen.
        private System.Timers.Timer IdleTimer = new System.Timers.Timer();

        // This is the timout for no interaction with the screen on an active session triggering a logout and reset
        private double IdleTimeout = 30;

        // This timer will handle how long the user has left to hit continue on timeout
        private static System.Timers.Timer ContinueTimer = new System.Timers.Timer();

        // After the user has been shown the wait screen this is the timeout for how long they have to hit continue
        private double ContinueTimeout = 30;

        // I will monitor if a session is active (someone has interacted with the screen
        private static bool ActiveSession = false;

        // I will store the display list currently in use by the program
        private DisplayList CurrentDisplayList = new DisplayList();

        readonly System.Windows.Threading.DispatcherTimer Timer = new System.Windows.Threading.DispatcherTimer();

        // Json file that contains the display data
        private static string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DisplayCarousel\\";
        private static string displayListFile = AppDataFolder + "displaylist.json";

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region Handling Active Sessions

            PreTrackSessionTimer.Interval = PreTrackSessionTimeout * 1000;
            PreTrackSessionTimer.Elapsed += PreTrackSessionTimer_Elapsed;
            PreTrackSessionTimer.Start();

            // Set up myself to capture events
            this.MouseDown += MainWindow_MouseDown;
            this.TouchDown += MainWindow_TouchDown;
            this.MouseMove += MainWindow_MouseMove;

            IdleTimer.Elapsed += IdleTimer_Elapsed;
            ContinueTimer.Elapsed += ContinueTimer_Elapsed;

            Timer.Tick += Timer_Click;
            Timer.Interval = new TimeSpan(0, 0, 1);
            Timer.Start();

            #endregion
            CreateAppDataFolder();
            LoadDisplayListFile(displayListFile);
        }

        private void CreateAppDataFolder()
        {
            if (!Directory.Exists(AppDataFolder))
                Directory.CreateDirectory(AppDataFolder);
        }

        #region Handling active sessions

        private void PreTrackSessionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (!TrackSession)
            {
                return;
            }

            PreTrackSession = false;
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            OnUserInteractionEvent();
        }

        private void MainWindow_TouchDown(object sender, TouchEventArgs e)
        {
            OnUserInteractionEvent();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OnUserInteractionEvent();
        }
        
        private void OnUserInteractionEvent()
        {

            if (!TrackSession || !PreTrackSession)
            {
                return;
            }

            // If there is an active session I want to reset my idle timer
            if (ActiveSession)
            {
                // Reset the timer
                IdleTimer.Interval = IdleTimeout * 1000;
            }
            else
            {

                ActiveSession = true;
                IdleTimer.Interval = IdleTimeout * 1000;
                IdleTimer.AutoReset = false;
                IdleTimer.Start();
            }
        }

        private void IdleTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            

            this.Dispatcher.Invoke(() =>
            {
                // Throw up a screen asking the user if they want to continue their session. 
                TimeoutScreen.Visibility = Visibility.Visible;
                ActiveWindow.Visibility = Visibility.Hidden;

                // start a timer to wait for them to hit continue
                ContinueTimer.Interval = ContinueTimeout * 1000;
                ContinueTimer.AutoReset = false;
                ContinueTimer.Start();
            });

        }

        private void ContinueTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            this.Dispatcher.Invoke(() =>
            {
                

                LoadDisplayList(CurrentDisplayList);
                ActiveSession = false;

                ActiveWindow.Visibility = Visibility.Visible;
                TimeoutScreen.Visibility = Visibility.Hidden;
            });
        }

        private void ContinueSessionButton_Click(object sender, RoutedEventArgs e)
        {
            ActiveWindow.Visibility = Visibility.Visible;
            TimeoutScreen.Visibility = Visibility.Hidden;

            ContinueTimer.Stop();

            IdleTimer.Interval = IdleTimeout * 1000;
        }

        private void TimeoutReset_Click(object sender, RoutedEventArgs e)
        {
            LoadDisplayList(CurrentDisplayList);
            ActiveSession = false;

            ActiveWindow.Visibility = Visibility.Visible;
            TimeoutScreen.Visibility = Visibility.Hidden;
        }

        #endregion


        private void LoadDisplayList(DisplayList list)
        {

            // remove anything currently in the display region
            foreach (UIElement child in DisplayRegion.Children)
            {
                Display display = child as Display;
                display.Destroy();
            }

            // Remove all the current children of the display region. 
            DisplayRegion.Children.RemoveRange(0,DisplayRegion.Children.Count);
            
            // set up all of the new displays
            foreach (Display display in list.Displays)
            {
                display.Setup();
                DisplayRegion.Children.Add(display);
            }

            SetDisplay(CurrentDisplayList.Displays.First()); 
        }

        #region Button Clicks

        // If the SelectDisplay button is clicked I will show the menu for selecting a site in the current site list
        private void SelectDisplayButton_Click(object sender, RoutedEventArgs e)
        {

            if (DisplaySelector.IsVisible)
            {
                DisplaySelector.Visibility = Visibility.Hidden;
            }
            else
            {
                DisplaySelector.Visibility = Visibility.Visible;
            }
        }
        
        // Exits the application when the exit button is clicked
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {

            if (MenuSelector.IsVisible)
            {
                MenuSelector.Visibility = Visibility.Hidden;
            }
            else
            {
                MenuSelector.Visibility = Visibility.Visible;
            }
        }

        // When the user clicks start looping it starts looping. 
        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {

            if (LoopButton.Content.Equals("Start Looping"))
            {
                LoopButton.Content = "Stop Looping";
                // First get the current display
                Display display = CurrentDisplayList.CurrentDisplay();

                // Set the view to that display
                SetDisplay(display);

                // Start the timer
                PageTimer = new System.Timers.Timer(display.Timer*1000);
                PageTimer.Elapsed += DisplayTimer_Elapsed;
                PageTimer.Start();
            }
            else
            {
                LoopButton.Content = "Start Looping";
                
                PageTimer.Stop();
            }
        }

        // Moves the user to the next display
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the next display
            Display display = CurrentDisplayList.NextDisplay();

            // Set the view to that display
            SetDisplay(display);
        }

        #endregion

        #region Handling displays

        // Handles the selection of displays via the display selector
        private void DisplaySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var newselection = e.AddedItems;
            var sent = sender;

            if (e.AddedItems.Count > 0)
            {
                CurrentDisplayList.SetIndex(e.AddedItems[0] as Display);
            }

            SetDisplay(CurrentDisplayList.CurrentDisplay());
            
            DisplaySelector.Visibility = Visibility.Hidden;

            PageTimer.Interval = CurrentDisplayList.CurrentDisplay().Timer * 1000;

        }
        
        // This sets the display region to show the target display
        private void SetDisplay(Display TargetDisplay)
        {
            foreach (FrameworkElement display in DisplayRegion.Children)
            {
                if (display.Equals(TargetDisplay))
                {
                    display.Visibility = Visibility.Visible;
                }
                else
                {
                    display.Visibility = Visibility.Hidden;
                }
            }
        }

        // Handles the Display Timer and moves between displays when it elapses
        private void DisplayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {

            // Get the next display
            Display display = CurrentDisplayList.NextDisplay();

            // Here I am just setting the view to the new display
            // I have to use this dispatcher thingy because the timer works on a different thread
            // Apparantly this causes issues with which thread owns the UI elements. 
            // this solves it but I have no idea why...
            // http://stackoverflow.com/questions/9732709/the-calling-thread-cannot-access-this-object-because-a-different-thread-owns-it
            this.Dispatcher.Invoke(() =>
            {
                SetDisplay(display);
            });

            // Reload the next display
            Display nextDisplay = CurrentDisplayList.QuietNextDisplay();
            this.Dispatcher.Invoke(() =>
            {
                nextDisplay.UpNext();
            });


            // Change the timer to the time for the new site
            PageTimer.Interval = display.Timer * 1000;

        }
        #endregion

        // This allows the user to load a display list.
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {

            // Here I used a template I stole from: 
            // http://stackoverflow.com/questions/10315188/open-file-dialog-and-select-a-file-using-wpf-controls-and-c-sharp

            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            
            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();
            
            // Get the selected file name and display in a TextBox 
            if (result.Equals(true))
            {
                // Open document 
                string filename = dlg.FileName;
                try
                {
                    LoadDisplayListFile(filename);
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Save the display list out to displaylist.json in the AppData folder
                try
                {
                    CurrentDisplayList.SaveToFile(displayListFile);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                
            }

            MenuSelector.Visibility = Visibility.Hidden;
            DisplayRegion.Visibility = Visibility.Visible;

        }

        // Load the JSON file
        private void LoadDisplayListFile(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }

            CurrentDisplayList = CurrentDisplayList.Load(filename);
            LoadDisplayList(CurrentDisplayList);
            DisplaySelector.ItemsSource = CurrentDisplayList.Displays;
            SetDisplay(CurrentDisplayList.CurrentDisplay());
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();

            MenuSelector.Visibility = Visibility.Hidden;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            //throw new NotImplementedException();

            MenuSelector.Visibility = Visibility.Hidden;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            LoadDisplayList(CurrentDisplayList);
        }

        private void Timer_Click(object sender, EventArgs e)
        {
 
            Clock.Content = DateTime.Now.ToString("t");
        }

    }
}
