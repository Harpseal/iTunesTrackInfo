using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using iTunesLib;

namespace iTunesTrackInfo
{
    public delegate void OnInitializationDoneEventEventHandler();
    public delegate void OnPlayerPlayEventEventHandler();
    public delegate void OnPlayerStopEventEventHandler();
    public delegate void OnDatabaseChangedEventHandler();
    public delegate void OnQuitEventHandler();

    class MusicPlayer
    {



        public enum MusicPlayerMode
        {
            Mode_External_iTunes = 0,
            Mode_BuildIn_WMP = 1
        }

        public enum MusicPlayerState
        {
            MusicPlayerStateStopped = 0,
            MusicPlayerStatePlaying = 1,
            MusicPlayerStateFastForward = 2,
            MusicPlayerStateRewind = 3,
        }

        public enum TrackStringInfo
        {
            Track_Album = 0,
            Track_Name,
            Track_Artist,
            Track_FullPath,
            Track_ArtworkPath,

            Track_StringInfo_Size
        }

        public enum TrackNumberInfo
        {
            Track_TrackNumber = TrackStringInfo.Track_StringInfo_Size,
            Track_DiscNumber,
            Track_Duration,

        }

        public float PlayerPosition
        {
            get { return getPlayerPosition(); }
            set { setPlayerPosition(value); }
        }

        public int PlayerState
        {
            get { return getPlayerState(); }
        }

        public MusicPlayer(MusicPlayerMode mode)
        {
        }
        ~MusicPlayer()
        {
        }


        public bool Play()
        {
            return true;
        }
        public bool Pause()
        {
            return true;
        }
        public bool Stop()
        {
            return true;
        }
        public bool NextTrack()
        {
            return true;
        }
        public bool PreviousTrack()
        {
            return true;
        }


        static event OnInitializationDoneEventEventHandler OnInitDoneEvent;
        static event OnPlayerPlayEventEventHandler OnPlayerPlayEvent;
        static event OnPlayerStopEventEventHandler OnPlayerStopEvent;
        static event OnDatabaseChangedEventHandler OnDatabaseChangedEvent;
        static event OnQuitEventHandler OnQuitEvent;





        public String getCurrentTrackInfo(TrackStringInfo info)
        {
            return "";
        }
        public float getCurrentNumberInfo(TrackNumberInfo info)
        {
            return -1;
        }



        public float getPlayerPosition()
        {
            return -1;
        }
        public void setPlayerPosition(float second)
        {

        }

        public int getPlayerState()
        {
            return (int)MusicPlayerState.MusicPlayerStateStopped;
        }


        private MusicPlayerMode m_playerMode = MusicPlayerMode.Mode_External_iTunes;
        private static iTunesApp m_iTunes = null;
        private _IiTunesEvents_OnPlayerPlayEventEventHandler itunesPlayerPlayEvent = new _IiTunesEvents_OnPlayerPlayEventEventHandler(iTunesOnPlayerPlayEvent);
        private _IiTunesEvents_OnPlayerStopEventEventHandler itunesPlayerStopEvent = new _IiTunesEvents_OnPlayerStopEventEventHandler(iTunesOnPlayerStopEvent);
        private _IiTunesEvents_OnDatabaseChangedEventEventHandler itunesDatabaseChangedEvent = new _IiTunesEvents_OnDatabaseChangedEventEventHandler(iTunesOnDatabaseChangedEvent);
        private _IiTunesEvents_OnQuittingEventEventHandler itunesQuittingEvent = new _IiTunesEvents_OnQuittingEventEventHandler(iTunesOnQuittingEvent);

        static void iTunesOnQuittingEvent()
        {
            //m_window.CloseByDispatcher();
            //...
            OnQuitEvent();
        }

        static void iTunesOnDatabaseChangedEvent(object deletedObjectIDs, object changedObjectIDs)
        {
            OnDatabaseChangedEvent();
        }
        static void iTunesOnPlayerPlayEvent(object iTrack)
        {
            OnPlayerPlayEvent();

            //m_window.m_iTunes.Pause();
            //MainWindow.m_window.btnPlay.Visibility = Visibility.Hidden;
            //MainWindow.m_window.btnPause.Visibility = Visibility.Visible;
            //...
        }

        static void iTunesOnPlayerStopEvent(object iTrack)
        {
            OnPlayerStopEvent();
        }
    }
}
