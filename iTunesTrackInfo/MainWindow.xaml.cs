using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using iTunesLib;
using System.Diagnostics;

using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media.Animation;

using iTunesLyrics;


namespace iTunesTrackInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        //private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName); 


        System.Windows.Forms.NotifyIcon m_trayNotifyIcon;
        static iTunesApp m_iTunes = null;
        private System.Windows.Point m_MousePosition;
        static MainWindow m_window;

        private static AutoResetEvent m_TickEvent = new AutoResetEvent(false);
        private static AutoResetEvent m_iTunesEvent = new AutoResetEvent(true);
        private static AutoResetEvent m_CloseEvent = new AutoResetEvent(false);

        private _IiTunesEvents_OnPlayerPlayEventEventHandler itunesPlayerPlayEvent = new _IiTunesEvents_OnPlayerPlayEventEventHandler(OnPlayerPlayEvent);
        private _IiTunesEvents_OnPlayerStopEventEventHandler itunesPlayerStopEvent = new _IiTunesEvents_OnPlayerStopEventEventHandler(OnPlayerStopEvent);
        private _IiTunesEvents_OnDatabaseChangedEventEventHandler itunesDatabaseChangedEvent = new _IiTunesEvents_OnDatabaseChangedEventEventHandler(OnDatabaseChangedEvent);
        private _IiTunesEvents_OnQuittingEventEventHandler itunesQuittingEvent = new _IiTunesEvents_OnQuittingEventEventHandler(OnQuittingEvent);

        private System.Windows.Forms.Timer m_timer;
        private Thread m_threadUpdate = null;

        Storyboard m_sbAniOut;
        Storyboard m_sbAniIn;

        UserActivityHook m_keyhook;
        bool m_isCtrlDown = false;
        bool m_isAltDown = false;
        bool m_isShiftDown = false;
        bool m_isKeyPauseResume = false;
        int m_iRatingNew = -1;
        string m_strPreTrackInfo;

        Mutex mutex;

        LyricsWindow m_lrcWindow;
        private const int m_refreshInterval = 1000;

        public MainWindow()
        {
            InitializeComponent();
            m_window = this;
            m_lrcWindow = new LyricsWindow();

            bool result;
            mutex = new System.Threading.Mutex(true, "iTunesTrackInfo_MainWindow", out result);

            if (!result)
            {
                MessageBox.Show("Another instance is already running.");
                this.Close();
                return;
            }

            //GC.KeepAlive(mutex);  

            m_MousePosition.X = m_MousePosition.Y = 0;

            m_trayNotifyIcon = new System.Windows.Forms.NotifyIcon();
            m_trayNotifyIcon.Icon = Properties.Resources.iTunes16;
            m_trayNotifyIcon.Visible = true;
            //m_trayNotifyIcon.DoubleClick +=
            //    delegate(object sender, EventArgs args)
            //    {
            //        this.Show();
            //        this.WindowState = WindowState.Normal;
            //        this.Activate();
            //        //this.ShowInTaskbar = !this.ShowInTaskbar;
            //        if (this.IsHitTestVisible == false)
            //        {
            //            this.IsHitTestVisible = true;
            //            this.m_sbAniIn.Begin(gridTotal);
            //        }
            //    };

            m_trayNotifyIcon.MouseClick +=
                delegate(object sender, System.Windows.Forms.MouseEventArgs args)
                {
                    if (args.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                        this.Activate();
                        
                        if (this.IsHitTestVisible == false)
                        {
                            this.IsHitTestVisible = true;
                            this.m_sbAniIn.Begin(gridTotal);
                        }
                    }
                    else if (args.Button == System.Windows.Forms.MouseButtons.Right)
                    {
                        this.ShowInTaskbar = !this.ShowInTaskbar;
                        m_lrcWindow.ShowInTaskbar = !m_lrcWindow.ShowInTaskbar;
                    }

                };


            Thread thread = new Thread(InitITunes);
            thread.Start();

            m_sbAniOut = new Storyboard();
            DoubleAnimation daFadeOut = new DoubleAnimation();
            daFadeOut.Duration = new TimeSpan(0, 0, 0, 0, 200);
            daFadeOut.To = 0.0;
            
            m_sbAniOut.Children.Add(daFadeOut);
            Storyboard.SetTargetProperty(daFadeOut, new PropertyPath(UIElement.OpacityProperty));

            m_sbAniIn = new Storyboard();
            DoubleAnimation daFadeIn = new DoubleAnimation();
            daFadeIn.Duration = new TimeSpan(0, 0, 0, 0, 200);
            daFadeIn.From = 0.0;
            daFadeIn.To = 1.0;

            m_sbAniIn.Children.Add(daFadeIn);
            Storyboard.SetTargetProperty(daFadeIn, new PropertyPath(UIElement.OpacityProperty));

            //imageTrackArtwrok.VisualBitmapScalingMode = System.Windows.Media.BitmapScalingMode.HighQuality;

            this.ShowInTaskbar = false;
            m_lrcWindow.ShowInTaskbar = false;


            m_timer = new System.Windows.Forms.Timer();
            m_timer.Interval = m_refreshInterval;
            m_timer.Tick += new EventHandler(Timer_Tick);
            m_timer.Start();

            spControlButtonPanel.Opacity = 0;
            spWindowButtonPanel.Opacity = 0;

            imageTrackArtwrok.Visibility = Visibility.Hidden;
            m_window.Topmost = true;

            m_strPreTrackInfo = "";


            try
            {
                Rect bounds = Rect.Parse(iTunesTrackInfo.Properties.Settings.Default.WindowRestoreBounds);
                int iInsideLT, iInsideRB;
                iInsideLT = iInsideRB = -1;
                for (int s = 0; s < System.Windows.Forms.Screen.AllScreens.Length; s++)
                {
                    System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[s];
                    if (bounds.Left >= screen.Bounds.Left && bounds.Top >= screen.Bounds.Top &&
                        bounds.Left < screen.Bounds.Right && bounds.Top < screen.Bounds.Bottom)
                    {
                        iInsideLT = s;
                    }

                    if (bounds.Right >= screen.Bounds.Left && bounds.Bottom >= screen.Bounds.Top &&
                        bounds.Right < screen.Bounds.Right && bounds.Bottom < screen.Bounds.Bottom)
                    {
                        iInsideRB = s;
                    }
                }
                if (iInsideLT != -1 || iInsideRB != -1)
                {
                    int recheckScreen = -1;

                    if (iInsideLT == -1)
                        recheckScreen = iInsideRB;
                    else if (iInsideRB == -1)
                        recheckScreen = iInsideLT;

                    if (recheckScreen != -1)
                    {
                        System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[recheckScreen];

                        if (bounds.X < screen.Bounds.Left)
                            bounds.X = screen.Bounds.Left;
                        if (bounds.Y < screen.Bounds.Top)
                            bounds.Y = screen.Bounds.Top;
                        if (bounds.Right > screen.Bounds.Right)
                            bounds.X -= bounds.Right - screen.Bounds.Right;
                        if (bounds.Bottom > screen.Bounds.Bottom)
                            bounds.Y -= bounds.Bottom - screen.Bounds.Bottom;

                    }
                    this.Top = bounds.Top;
                    this.Left = bounds.Left;
                    //this.Width = bounds.Width;
                    //this.Height = bounds.Height;
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("[" + TomatoTimerWPF.Properties.Settings.Default.WindowRestoreBounds + "]");
            }

            try
            {
                Rect bounds = Rect.Parse(iTunesTrackInfo.Properties.Settings.Default.LyricsRestoreBounds);
                int iInsideLT, iInsideRB;
                iInsideLT = iInsideRB = -1;
                for (int s = 0; s < System.Windows.Forms.Screen.AllScreens.Length; s++)
                {
                    System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[s];
                    if (bounds.Left >= screen.Bounds.Left && bounds.Top >= screen.Bounds.Top &&
                        bounds.Left < screen.Bounds.Right && bounds.Top < screen.Bounds.Bottom)
                    {
                        iInsideLT = s;
                    }

                    if (bounds.Right >= screen.Bounds.Left && bounds.Bottom >= screen.Bounds.Top &&
                        bounds.Right < screen.Bounds.Right && bounds.Bottom < screen.Bounds.Bottom)
                    {
                        iInsideRB = s;
                    }
                }
                if (iInsideLT != -1 || iInsideRB != -1)
                {
                    int recheckScreen = -1;

                    if (iInsideLT == -1)
                        recheckScreen = iInsideRB;
                    else if (iInsideRB == -1)
                        recheckScreen = iInsideLT;

                    if (recheckScreen != -1)
                    {
                        System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.AllScreens[recheckScreen];

                        if (bounds.X < screen.Bounds.Left)
                            bounds.X = screen.Bounds.Left;
                        if (bounds.Y < screen.Bounds.Top)
                            bounds.Y = screen.Bounds.Top;
                        if (bounds.Right > screen.Bounds.Right)
                            bounds.X -= bounds.Right - screen.Bounds.Right;
                        if (bounds.Bottom > screen.Bounds.Bottom)
                            bounds.Y -= bounds.Bottom - screen.Bounds.Bottom;

                    }
                    m_lrcWindow.Top = bounds.Top;
                    m_lrcWindow.Left = bounds.Left;
                    m_lrcWindow.Width = bounds.Width;
                    m_lrcWindow.Height = bounds.Height;
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("[" + TomatoTimerWPF.Properties.Settings.Default.WindowRestoreBounds + "]");
            }
        }

        public void keyhook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //MessageBox.Show("KeyDown:" + e.KeyCode);

            if (e.KeyCode == System.Windows.Forms.Keys.LShiftKey || e.KeyCode == System.Windows.Forms.Keys.RShiftKey)
            {
                m_isShiftDown = true;
                if (m_window.IsHitTestVisible == false)
                {
                    m_window.IsHitTestVisible = true;
                    m_sbAniIn.Begin(gridTotal);
                }

            }

            if (e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                m_isCtrlDown = true;
            }

            if (e.KeyCode == System.Windows.Forms.Keys.LMenu || e.KeyCode == System.Windows.Forms.Keys.RMenu)
            {
                m_isAltDown = true;
            }

            
            Debug.WriteLine("KeyDown:" + e.KeyCode);

            if (m_iTunes != null && m_isCtrlDown)
            {

                m_iRatingNew = -1;
                if (e.KeyCode == System.Windows.Forms.Keys.Insert || e.KeyCode == System.Windows.Forms.Keys.NumPad0)
                    m_iRatingNew = (m_isAltDown) ? 10 : 0;
                if (e.KeyCode == System.Windows.Forms.Keys.End || e.KeyCode == System.Windows.Forms.Keys.NumPad1)
                    m_iRatingNew = (m_isAltDown) ? 30 : 20;
                if (e.KeyCode == System.Windows.Forms.Keys.Down || e.KeyCode == System.Windows.Forms.Keys.NumPad2)
                    m_iRatingNew = (m_isAltDown) ? 50 : 40;
                if (e.KeyCode == System.Windows.Forms.Keys.Next || e.KeyCode == System.Windows.Forms.Keys.NumPad3)
                    m_iRatingNew = (m_isAltDown) ? 70 : 60;
                if (e.KeyCode == System.Windows.Forms.Keys.Left || e.KeyCode == System.Windows.Forms.Keys.NumPad4)
                    m_iRatingNew = (m_isAltDown) ? 90 : 80;
                if (e.KeyCode == System.Windows.Forms.Keys.Clear || e.KeyCode == System.Windows.Forms.Keys.NumPad5)
                    m_iRatingNew = 100;
                if (m_iRatingNew >= 0)
                    e.Handled = true;
                m_TickEvent.Set();
                //m_trayNotifyIcon.ShowBalloonTip(1000, "KeyDown", "KeyDown:" + e.KeyCode + " " + m_iRatingNew + " ", System.Windows.Forms.ToolTipIcon.Info);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Clear)
            {
                m_isKeyPauseResume = true;
                e.Handled = true;

            }

           
            //m_trayNotifyIcon.ShowBalloonTip(1000, "KeyDown", "KeyDown:" + e.KeyCode + " " + m_isCtrlDown + " ", System.Windows.Forms.ToolTipIcon.Info);
        }

        public void keyhook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //m_trayNotifyIcon.ShowBalloonTip(1000, "KeyUp", "KeyUp:" + e.KeyCode, System.Windows.Forms.ToolTipIcon.Info);
            //MessageBox.Show("KeyUp:" + e.KeyCode);
            Debug.WriteLine("KeyUp:" + e.KeyCode);
            if (e.KeyCode == System.Windows.Forms.Keys.LShiftKey || e.KeyCode == System.Windows.Forms.Keys.RShiftKey)
            {
                m_isShiftDown = false;
                if (m_window.IsHitTestVisible == false)
                {
                    m_window.IsHitTestVisible = true;
                    m_sbAniIn.Begin(gridTotal);
                }
            }

            if (e.KeyCode == System.Windows.Forms.Keys.LControlKey || e.KeyCode == System.Windows.Forms.Keys.RControlKey)
            {
                m_isCtrlDown = false;
            }
            if (e.KeyCode == System.Windows.Forms.Keys.LMenu || e.KeyCode == System.Windows.Forms.Keys.RMenu)
            {
                m_isAltDown = false;
            }
        }

        public void keyhook_MouseActivity(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!m_window.IsHitTestVisible)
            {
                if (e.X < m_window.RestoreBounds.Left || e.X > m_window.RestoreBounds.Right || e.Y < m_window.RestoreBounds.Top || e.Y > m_window.RestoreBounds.Bottom)
                {
                    m_window.IsHitTestVisible = true;
                    m_sbAniIn.Begin(gridTotal);
                    //m_trayNotifyIcon.ShowBalloonTip(1000, "MouseActivity", "MouseActivity : mouse left.", System.Windows.Forms.ToolTipIcon.Info);
                }

            }
        }


        void Timer_Tick(object sender, EventArgs e)
        {
            m_TickEvent.Set();
        }

        void UpdateUI()
        {
            int iHandle = 0;
            WaitHandle[] waitHandes = new WaitHandle[] { m_TickEvent, m_iTunesEvent, m_CloseEvent };
            try
            {
                while ((iHandle = WaitHandle.WaitAny(waitHandes)) != 2)
                {
                    if (m_iTunes == null)
                        continue;

                    if (iHandle == 0)//m_TickEvent
                    {
                        m_window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                        {

                            switch (m_iTunes.PlayerState)
                            {
                                case ITPlayerState.ITPlayerStatePlaying:
                                    TimeSpan time = new TimeSpan(0, 0, m_iTunes.PlayerPosition);

                                    m_lrcWindow.setSecondPositon((float)time.TotalSeconds, m_refreshInterval);

                                    if (time.Hours != 0)
                                        labelTrackTimeCurrent.Content = time.ToString(@"hh\:mm\:ss");
                                    else
                                        labelTrackTimeCurrent.Content = time.ToString(@"mm\:ss");
                                    labelStatus.Content = "Playing";
                                    btnPlay.Visibility = Visibility.Collapsed;
                                    btnPause.Visibility = Visibility.Visible;
                                    double process = (double)m_iTunes.PlayerPosition * 100.0 / (double)Math.Max(1, m_iTunes.CurrentTrack.Duration);
                                    pbarTrackTime.Value = Math.Min(100.0, process);
                                    pbarTrackTime.IsIndeterminate = false;

                                    if (m_iRatingNew>=0)
                                    {
                                        m_iTunes.CurrentTrack.Rating = m_iRatingNew;
                                        m_iRatingNew = -1;
                                    }
                                    if (m_isKeyPauseResume)
                                    {
                                        m_iTunes.Pause();
                                        m_isKeyPauseResume = false;
                                    }
                                    
                                    break;
                                case ITPlayerState.ITPlayerStateStopped:
                                    m_iRatingNew = -1;
                                    labelStatus.Content = "Stopped";                                  
                                    btnPlay.Visibility = Visibility.Visible;
                                    btnPause.Visibility = Visibility.Collapsed;
                                    if (m_isKeyPauseResume)
                                    {
                                        m_iTunes.Play();
                                        m_isKeyPauseResume = false;
                                    }
                                    break;
                                case ITPlayerState.ITPlayerStateRewind:
                                    labelStatus.Content = "Rewind";
                                    break;
                                case ITPlayerState.ITPlayerStateFastForward:
                                    labelStatus.Content = "FastForward";
                                    break;
                                default:
                                    labelStatus.Content = "Unknown";
                                    break;
                            }
                            
                            return null;
                        }), null);


                        Debug.WriteLine("m_TickEvent");
                    }
                    else if (iHandle == 1)//m_iTunesEvent
                    {
                        m_window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
                        {
                            try
                            {

                                IITFileOrCDTrack iIFileTrack = m_iTunes.CurrentTrack as IITFileOrCDTrack;

                                string curTrackInfo;
                                labelAlbum.Content = m_iTunes.CurrentTrack.Album;
                                labelTrackName.Content = m_iTunes.CurrentTrack.TrackNumber + "." + m_iTunes.CurrentTrack.Name;
                                labelArtist.Content = m_iTunes.CurrentTrack.Artist;

                                if (iIFileTrack != null)
                                    curTrackInfo = System.IO.Path.GetDirectoryName(iIFileTrack.Location);
                                else
                                    curTrackInfo = m_iTunes.CurrentTrack.Album + ", " + m_iTunes.CurrentTrack.DiscNumber;
                                Debug.WriteLine("Pre : [" + m_strPreTrackInfo + "]");
                                Debug.WriteLine("Cur : [" + curTrackInfo + "]");

                                
                                

                                bool isLoadSuccess = (m_strPreTrackInfo == curTrackInfo);

                                if (iIFileTrack != null)
                                {
                                    string trackDirectory = System.IO.Path.GetDirectoryName(iIFileTrack.Location);
                                    string trackName = System.IO.Path.GetFileNameWithoutExtension(iIFileTrack.Location);
                                    Console.WriteLine("GetFileNameWithoutExtension : [" + trackDirectory + "\\" + trackName + "]");
                                    m_lrcWindow.clear();
                                    if (m_lrcWindow.load(trackDirectory + "\\" + trackName + ".lrc"))
                                    {
                                        m_lrcWindow.rebuildLyricsUI();
                                        m_lrcWindow.Show();
                                        btnLyric.Visibility = Visibility.Visible;
                                    }
                                    else if (m_lrcWindow.load(trackDirectory + "\\" + trackName + ".ass"))
                                    {
                                        m_lrcWindow.rebuildLyricsUI();
                                        m_lrcWindow.Show();
                                        btnLyric.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        m_lrcWindow.Hide();
                                        btnLyric.Visibility = Visibility.Collapsed;
                                    }



                                    string[] defaultArtworkName = new string[] {"folder.jpg", "cover.png", "cover.jpg", "folder.png" };
                                    
                                    string trackCoverFile;
                                    for (int i = 0; i < defaultArtworkName.Length && !isLoadSuccess; i++)
                                    {
                                        trackCoverFile = trackDirectory + "\\" + defaultArtworkName[i];
                                        Console.WriteLine("trackCoverFile " + System.IO.File.Exists(trackCoverFile) + " : " + trackCoverFile);
                                        if (System.IO.File.Exists(trackCoverFile))
                                        {
                                            try
                                            {
                                                var bitmap = new BitmapImage();
                                                bitmap.BeginInit();
                                                bitmap.UriSource = new Uri(trackCoverFile, UriKind.Absolute);
                                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                                bitmap.EndInit();

                                                imageTrackArtwrok.Source = bitmap;
                                                imageTrackArtwrok.Opacity = 1;
                                                imageTrackArtwrok.Visibility = Visibility.Visible;
                                                isLoadSuccess = true;
                                            }
                                            catch (System.SystemException e)//System.NotSupportedException + System.ArgumentException
                                            {
                                                Console.WriteLine("trackCoverFile SystemException!!" + e.ToString());
                                                isLoadSuccess = false;
                                            }
                                        }
                                    }
                                }


                                if (!isLoadSuccess)
                                {
                                    IITArtworkCollection artworkCollection = m_iTunes.CurrentTrack.Artwork;
                                    if (artworkCollection.Count == 0)
                                    {
                                        imageTrackArtwrok.Visibility = Visibility.Hidden;
                                    }
                                    else
                                    {
                                        string artworkPath = System.IO.Path.GetTempPath() + "iTunesTrackInfo_Artwork";
                                        IITArtwork artwork = artworkCollection[1];
                                        switch (artwork.Format)
                                        {
                                            case ITArtworkFormat.ITArtworkFormatBMP:
                                                artworkPath += ".BMP";
                                                break;
                                            case ITArtworkFormat.ITArtworkFormatPNG:
                                                artworkPath += ".PNG";
                                                break;
                                            case ITArtworkFormat.ITArtworkFormatJPEG:
                                                artworkPath += ".JPG";
                                                break;
                                            default:
                                                artworkPath = "";
                                                break;
                                        }
                                        if (artworkPath.Length != 0 && m_strPreTrackInfo != curTrackInfo)
                                        {
                                            try
                                            {
                                                imageTrackArtwrok.Source = null;
                                                artwork.SaveArtworkToFile(artworkPath);

                                                try
                                                {
                                                    var bitmap = new BitmapImage();
                                                    bitmap.BeginInit();
                                                    bitmap.UriSource = new Uri(artworkPath, UriKind.Absolute);
                                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                                    bitmap.EndInit();

                                                    imageTrackArtwrok.Source = bitmap;
                                                    imageTrackArtwrok.Visibility = Visibility.Visible;
                                                    imageTrackArtwrok.Opacity = 1;
                                                }
                                                catch (System.SystemException)//System.NotSupportedException + System.ArgumentException
                                                {
                                                    imageTrackArtwrok.Visibility = Visibility.Hidden;
                                                }
                                            }
                                            catch (System.UnauthorizedAccessException)
                                            {
                                                Debug.WriteLine("UnauthorizedAccessException");
                                                imageTrackArtwrok.Visibility = Visibility.Hidden;
                                            }


                                        }
                                        else
                                            Debug.WriteLine("Skip to change Artwork.");
                                        Debug.WriteLine("ArtworkPath[" + artworkPath + "]");




                                        //artworkCollection[0].SaveArtworkToFile();

                                    }
                                }

                                m_strPreTrackInfo = curTrackInfo;

                                TimeSpan time = new TimeSpan(0, 0, m_iTunes.CurrentTrack.Duration);
                                if (time.Hours != 0)
                                    labelTrackTimeTotal.Content = time.ToString(@"hh\:mm\:ss");
                                else
                                    labelTrackTimeTotal.Content = time.ToString(@"mm\:ss");

                                int rating = m_iTunes.CurrentTrack.Rating;
                                labelRating.Content = "";
                                for (int r = 0; r < 5; r++)
                                {
                                    if (rating >= 20)
                                        labelRating.Content += "★";
                                    else if (rating >= 10)
                                        labelRating.Content += "½";
                                    else
                                        labelRating.Content += "☆";
                                    rating -= 20;

                                }
                            }
                            catch (System.NullReferenceException)
                            {

                            }
                            return null;
                        }), null);

                        Debug.WriteLine("m_iTunesEvent");

                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException come)
            {
                m_window.CloseByDispatcher();
            }
            Debug.WriteLine("UpdateUI Closing");

        }

        public void CloseByDispatcher()
        {
            m_window.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new System.Windows.Threading.DispatcherOperationCallback(delegate
            {
                m_window.Close();
                return null;
            }), null);
        }

        void InitITunes()
        {
            if (m_iTunes != null) return;
            m_trayNotifyIcon.Text = "Start to connecting to iTunes...";
            try
            {
                m_iTunes = new iTunesApp();
            }
            catch (System.Runtime.InteropServices.COMException come)
            {
                MessageBox.Show("Error Information: " + come.ToString(),"iTunes Error",MessageBoxButton.OK,MessageBoxImage.Error);
                this.Close();
                return;
            }

            m_iTunes.OnPlayerPlayEvent += itunesPlayerPlayEvent;
            m_iTunes.OnPlayerStopEvent += itunesPlayerStopEvent;
            m_iTunes.OnDatabaseChangedEvent += itunesDatabaseChangedEvent;
            m_iTunes.OnQuittingEvent += itunesQuittingEvent;

            m_trayNotifyIcon.Text = "iTunes is connected.";
            m_trayNotifyIcon.ShowBalloonTip(100, "iTunesTrackInfo", "iTunes is connected.", System.Windows.Forms.ToolTipIcon.Info);

            m_threadUpdate = new Thread(UpdateUI);
            m_threadUpdate.Start();

            
            //m_window.Show();
            //m_window.Activate();
            //this.Activate();

        }

        static void OnQuittingEvent()
        {
            m_window.CloseByDispatcher();
            //...
        }

        static void OnDatabaseChangedEvent(object deletedObjectIDs, object changedObjectIDs)
        {
            Debug.WriteLine("OnDatabaseChangedEvent ");
            m_iTunesEvent.Set();
        }
        static void OnPlayerPlayEvent(object iTrack)
        {
            IITTrack track = (IITTrack)iTrack;
            Debug.WriteLine("OnPlayerPlayEvent " + (track != null) + " " + track.Rating + " " + track.Album + " " + track.Name);
            m_iTunesEvent.Set();
            
            //m_window.m_iTunes.Pause();
            //MainWindow.m_window.btnPlay.Visibility = Visibility.Hidden;
            //MainWindow.m_window.btnPause.Visibility = Visibility.Visible;
            //...
        }

        static void OnPlayerStopEvent(object iTrack)
        {
            IITTrack track = (IITTrack)iTrack;
            Debug.WriteLine("OnPlayerStopEvent " + (track != null) + " " + track.Rating + " " + track.Album + " " + track.Name);
            //m_iTunesEvent.Set();
            //MainWindow.m_window.btnPlay.Visibility = Visibility.Visible;
            //MainWindow.m_window.btnPause.Visibility = Visibility.Hidden;
            //...
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            

            m_CloseEvent.Set();

            Properties.Settings.Default.WindowRestoreBounds = this.RestoreBounds.ToString();
            Properties.Settings.Default.LyricsRestoreBounds = m_lrcWindow.RestoreBounds.ToString();
            Properties.Settings.Default.Save();

            if (m_trayNotifyIcon != null)
            {
                m_trayNotifyIcon.Dispose();
            }
            if (m_iTunes != null)
            {
                m_iTunes.OnPlayerPlayEvent -= itunesPlayerPlayEvent;
                m_iTunes.OnPlayerStopEvent -= itunesPlayerStopEvent;
                m_iTunes.OnDatabaseChangedEvent -= itunesDatabaseChangedEvent;
                m_iTunes.OnQuittingEvent -= itunesQuittingEvent;

                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_iTunes);
                m_iTunes = null;
            }
            if (m_threadUpdate != null && m_threadUpdate.IsAlive)
            {
                m_threadUpdate.Abort();
            }

            m_lrcWindow.Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void btnMove_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_MousePosition = e.GetPosition(btnMove);

            //if (m_window.WindowState == WindowState.Maximized)
            //{
            //    double borderWidth;
            //    double borderHeight;

            //    //The real border size is 7 in my windows 7, but ResizeFrameVerticalBorderWidth is 4 ???
            //    //borderWidth = SystemParameters.ResizeFrameVerticalBorderWidth;
            //    //borderHeight = SystemParameters.ResizeFrameHorizontalBorderHeight;

            //    //use window api to get the real border size.
            //    var rectWindow = GetWindowRectangle(m_window);
            //    Point ptClient = m_window.PointToScreen(new Point(0, 0));
            //    borderWidth = ptClient.X - rectWindow.Left;
            //    borderHeight = ptClient.Y - rectWindow.Top;

            //    m_window.WindowState = WindowState.Normal;

            //    System.Drawing.Point pointMouse = System.Windows.Forms.Control.MousePosition;
            //    Point relativePoint = btnMove.TransformToAncestor(m_window)
            //                      .Transform(m_MousePosition);
            //    m_window.Left = (double)pointMouse.X - relativePoint.X - borderWidth;
            //    m_window.Top = (double)pointMouse.Y - relativePoint.Y - borderHeight;
            //}
        }

       private void btnMove_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            //MessageBox.Show("MM");
            var currentPoint = e.GetPosition(btnMove);
            if (e.LeftButton == MouseButtonState.Pressed
                &&
                btnMove.IsMouseCaptured &&
                (Math.Abs(currentPoint.X - m_MousePosition.X) >
                    SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(currentPoint.Y - m_MousePosition.Y) >
                    SystemParameters.MinimumVerticalDragDistance))
            {
                // Prevent Click from firing
                btnMove.ReleaseMouseCapture();
                this.DragMove();
            }



        }

       private void btnNext_Click(object sender, RoutedEventArgs e)
       {
           if (m_iTunes != null)
               m_iTunes.NextTrack();
       }

       private void btnPlay_Click(object sender, RoutedEventArgs e)
       {
           if (m_iTunes != null)
               m_iTunes.Play();
       }

       private void btnPause_Click(object sender, RoutedEventArgs e)
       {
           if (m_iTunes != null)
               m_iTunes.Pause();
       }

       private void btnPrevious_Click(object sender, RoutedEventArgs e)
       {
           if (m_iTunes != null)
               m_iTunes.PreviousTrack();
       }

       private void Window_MouseEnter(object sender, MouseEventArgs e)
       {
           if (m_isShiftDown || m_keyhook==null)
           {
               m_sbAniIn.Begin(spControlButtonPanel);
               m_sbAniIn.Begin(spWindowButtonPanel);
           }
           else
           {
               if (m_window.IsHitTestVisible)
               {
                   m_window.IsHitTestVisible = false;
                   m_sbAniOut.Begin(gridTotal);
               }
           }
       }

       private void Window_MouseLeave(object sender, MouseEventArgs e)
       {
           
           {
               m_sbAniOut.Begin(spControlButtonPanel);
               m_sbAniOut.Begin(spWindowButtonPanel);
           }


       }

       private void Window_Loaded(object sender, RoutedEventArgs e)
       {
           try
           {
               m_keyhook = new UserActivityHook();
               m_keyhook.KeyDown += new System.Windows.Forms.KeyEventHandler(keyhook_KeyDown);
               m_keyhook.KeyUp += new System.Windows.Forms.KeyEventHandler(keyhook_KeyUp);
               m_keyhook.OnMouseActivity += new System.Windows.Forms.MouseEventHandler(keyhook_MouseActivity);
           }
           catch (System.ComponentModel.Win32Exception)
           {
               m_keyhook = null;
           }
       }

       private void btnLyric_Click(object sender, RoutedEventArgs e)
       {
           if (m_lrcWindow.getLrcCount() != 0)
               m_lrcWindow.Show();
       }

    }
}
