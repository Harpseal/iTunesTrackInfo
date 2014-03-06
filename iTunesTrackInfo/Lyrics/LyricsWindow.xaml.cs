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
using System.Windows.Shapes;

using System.Threading;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

using ScrollAnimateBehavior;

namespace iTunesLyrics
{
    /// <summary>
    /// Interaction logic for LyricsWindow.xaml
    /// </summary>
    public partial class LyricsWindow : Window
    {
        private LyricsReader m_lrcReader;
        private int m_iPreLrc;

        Storyboard m_sbAniSmoothScroll;
        DoubleAnimation m_daSmoothScroll;

        int m_iPreHitLyrics;
        int m_iAniHitLyrics;
        Storyboard m_sbAniTextColorIn;
        Storyboard m_sbAniTextColorOut;
        ColorAnimation m_caTextColorIn;
        ColorAnimation m_caTextColorOut;

        Color m_colorForeground;
        Color m_colorBackground;
        

        public LyricsWindow()
        {
            InitializeComponent();
            m_lrcReader = new LyricsReader();
            m_iPreLrc = 0;

            m_colorForeground = Color.FromArgb(255, 0, 0, 0);
            m_colorBackground = Color.FromArgb(180, 0, 0, 0);

            m_sbAniSmoothScroll = new Storyboard();
            m_daSmoothScroll = new DoubleAnimation();

            m_sbAniSmoothScroll.Children.Add(m_daSmoothScroll);
            Storyboard.SetTarget(m_daSmoothScroll, svLyricsScroll);
            Storyboard.SetTargetProperty(m_daSmoothScroll, new PropertyPath(ScrollAnimateBehavior.AttachedBehaviors.ScrollAnimationBehavior.VerticalOffsetProperty));

            m_sbAniTextColorIn = new Storyboard();
            m_caTextColorIn = new ColorAnimation();
            m_caTextColorIn.Duration = new TimeSpan(0, 0, 0, 0, 200);
            //m_caTextColorIn.From = m_colorBackground;
            m_caTextColorIn.To = m_colorForeground;
            m_sbAniTextColorIn.Children.Add(m_caTextColorIn);
            Storyboard.SetTargetProperty(m_caTextColorIn, new PropertyPath(SolidColorBrush.ColorProperty));


            m_sbAniTextColorOut = new Storyboard();
            m_caTextColorOut = new ColorAnimation();
            m_caTextColorOut.Duration = new TimeSpan(0, 0, 0, 0, 200);
            //m_caTextColorOut.From = m_colorForeground;
            m_caTextColorOut.To = m_colorBackground;
            m_sbAniTextColorOut.Children.Add(m_caTextColorOut);
            Storyboard.SetTargetProperty(m_caTextColorOut, new PropertyPath(SolidColorBrush.ColorProperty));

            m_iPreHitLyrics = -1;
            m_iAniHitLyrics = -1;



            //.VerticalOffsetProperty
            
        }

        public void rebuildLyricsUI()
        {
            spLyricsMain.Children.Clear();
            if (m_lrcReader.m_listLyricMain == null) return;

            // Initialize a new DropShadowBitmapEffect that will be applied
            // to the Button.
            DropShadowEffect myDropShadowEffect = new DropShadowEffect();
            // Set the color of the shadow to Black.
            Color myShadowColor = new Color();
            myShadowColor.ScA = 1;
            myShadowColor.ScB = 0.4f;
            myShadowColor.ScG = 0.4f;
            myShadowColor.ScR = 0.4f;
            myDropShadowEffect.Color = myShadowColor;

            // Set the direction of where the shadow is cast to 320 degrees.
            myDropShadowEffect.Direction = 320;

            // Set the depth of the shadow being cast.
            myDropShadowEffect.ShadowDepth = 0;

            // Set the shadow softness to the maximum (range of 0-1).
            //myDropShadowEffect.Softness = 1;
            // Set the shadow opacity to half opaque or in other words - half transparent.
            // The range is 0-1.
            myDropShadowEffect.Opacity = 1; 

            foreach (LyricsItem item in m_lrcReader.m_listLyricMain)
            {
                TextBox tbox = new TextBox();
                //tbox.Text = item.ToString();
                tbox.Text = item.strLyrics;
                tbox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                //Binding bnd = new Binding("Value") { ElementName = "spLyricsMain" };
                //tbox.SetBinding(FrameworkElement.WidthProperty, bnd);
                tbox.TextWrapping = TextWrapping.Wrap;
                tbox.TextAlignment = TextAlignment.Center;
                tbox.BorderThickness = new Thickness(0, 0, 0, 0); ;
                tbox.Background = null;
                tbox.Foreground = new SolidColorBrush(m_colorBackground);
                tbox.FontSize = 16;
                tbox.Effect = myDropShadowEffect;

                spLyricsMain.Children.Add(tbox);
            }
        }

        public bool load(string path)
        {
            return m_lrcReader.load(path);
        }

        public bool mergeAll()
        {
            return m_lrcReader.mergeAll();
        }

        public void clear()
        {
            svLyricsScroll.ScrollToVerticalOffset(0);
            m_iPreHitLyrics = -1;
            m_iAniHitLyrics = -1;

            m_lrcReader.clear();
            spLyricsMain.Children.Clear();
        }

        public void setSecondPositon(float second,int aniMSec=-1)
        {
            int idx = 0, idxAni = 0 ;
            float ratio = 0;
            bool isHit;
            isHit = getLrcRatioBySecond(second, ref idx, ref ratio);

            if (idx>=0 && idx<spLyricsMain.Children.Count)
            {
                TextBox tbox = spLyricsMain.Children[idx] as TextBox;
                Point offset = tbox.TranslatePoint(new Point(0, 0), svLyricsScroll);

                if (idx != m_iPreHitLyrics)
                {
                    tbox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, m_caTextColorIn);
                    //m_sbAniTextColorIn.Begin(tbox.Foreground);
                    if (m_iPreHitLyrics>=0 && m_iPreHitLyrics<spLyricsMain.Children.Count)
                        (spLyricsMain.Children[m_iPreHitLyrics] as TextBox).Foreground.BeginAnimation(SolidColorBrush.ColorProperty, m_caTextColorOut);
                        //m_sbAniTextColorOut.Begin((spLyricsMain.Children[m_iPreHitLyrics] as TextBox).Foreground);
                }
                else if (!isHit)
                {
                    if (m_iPreHitLyrics >= 0 && m_iPreHitLyrics < spLyricsMain.Children.Count)
                        (spLyricsMain.Children[m_iPreHitLyrics] as TextBox).Foreground.BeginAnimation(SolidColorBrush.ColorProperty, m_caTextColorOut);
                }

                //System.Console.WriteLine("lyrics : " + m_iLyricsCount + " (" + offset.X + "," + (offset.Y + svLyricsScroll.VerticalOffset) + ")  " + svLyricsScroll.VerticalOffset + " " + svLyricsScroll.ScrollableHeight);

                svLyricsScroll.ScrollToVerticalOffset(Math.Max((offset.Y + svLyricsScroll.VerticalOffset) - svLyricsScroll.ViewportHeight / 2 + tbox.ActualHeight * ratio, 0));
                if (aniMSec > 0)
                {
                    getLrcRatioBySecond(second+(float)aniMSec/1000.0f, ref idxAni, ref ratio);

                    tbox = spLyricsMain.Children[idxAni] as TextBox;
                    offset = tbox.TranslatePoint(new Point(0, 0), svLyricsScroll);
                    m_daSmoothScroll.Duration = new TimeSpan(0, 0, 0, 0, aniMSec);
                    //m_daSmoothScroll.From = svLyricsScroll.VerticalOffset;
                    m_daSmoothScroll.To = Math.Max((offset.Y + svLyricsScroll.VerticalOffset) - svLyricsScroll.ViewportHeight / 2 + tbox.ActualHeight * ratio, 0);
                    m_sbAniSmoothScroll.Begin();

                    if (idxAni != m_iAniHitLyrics)
                    {
                        tbox.Foreground.BeginAnimation(SolidColorBrush.ColorProperty, m_caTextColorIn);
                        if (m_iAniHitLyrics != m_iPreHitLyrics && m_iPreHitLyrics >= 0 && m_iPreHitLyrics < spLyricsMain.Children.Count)
                            (spLyricsMain.Children[m_iAniHitLyrics] as TextBox).Foreground.BeginAnimation(SolidColorBrush.ColorProperty, m_caTextColorOut);
                    }

                }
                m_iPreHitLyrics = idx;
                m_iAniHitLyrics = idxAni;
            }

        }

        public int getLrcCount()
        {
            if (m_lrcReader.m_listLyricMain == null) return 0;
            return m_lrcReader.m_listLyricMain.Count;
        }

        public string getLrcTextByIndex(int lrcIndex)
        {
            if (m_lrcReader.m_listLyricMain == null) return string.Empty;
            if (lrcIndex < 0 || lrcIndex >= m_lrcReader.m_listLyricMain.Count) return string.Empty;
            return m_lrcReader.m_listLyricMain[lrcIndex].strLyrics;
        }

        public bool getLrcRatioBySecond(float second,ref int index,ref float ratio)
        {
            if (m_lrcReader.m_listLyricMain == null)
            {
                index = 0;
                ratio = 0;
                return false;
            }

            if (second < 10 || m_iPreLrc<0)
                m_iPreLrc = 0;
            if (m_iPreLrc >= m_lrcReader.m_listLyricMain.Count)
                m_iPreLrc = m_lrcReader.m_listLyricMain.Count -1;

            int searchDir;
            if (second >= m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart)
                searchDir = 1;
            else
                searchDir = -1;

            for (; m_iPreLrc < m_lrcReader.m_listLyricMain.Count && m_iPreLrc>=0; m_iPreLrc+=searchDir)
            {
                if (m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampEnd == -1)
                {
                    if (m_iPreLrc == m_lrcReader.m_listLyricMain.Count - 1)
                    {
                        index = m_iPreLrc;
                        ratio = 0;
                        return true;
                    }
                    else if (second >= m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart && second < m_lrcReader.m_listLyricMain[m_iPreLrc + 1].fTimeStampStart)
                    {
                        index = m_iPreLrc;
                        ratio = (second - m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart) / (m_lrcReader.m_listLyricMain[m_iPreLrc + 1].fTimeStampStart - m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart);
                        return true;
                    }
                }
                else if (second >= m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart && second < m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampEnd)
                {
                    index = m_iPreLrc;
                    ratio = (second - m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart) / (m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampEnd - m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart);
                    return true;
                }
                else if (searchDir > 0 && second < m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampStart)
                {
                    index = m_iPreLrc;
                    ratio = 0;
                    return false;
                }
                else if (searchDir < 0 && second > m_lrcReader.m_listLyricMain[m_iPreLrc].fTimeStampEnd)
                {
                    index = m_iPreLrc;
                    ratio = 1;
                    return false;
                }
            }

            if (m_iPreLrc < 0)
                m_iPreLrc = 0;
            if (m_iPreLrc >= m_lrcReader.m_listLyricMain.Count)
                m_iPreLrc = m_lrcReader.m_listLyricMain.Count -1;
            index = m_iPreLrc;
            ratio = 0;
            return false;

        }

        private void btnMove_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            (sender as Button).ReleaseMouseCapture();
            this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

    }
}
