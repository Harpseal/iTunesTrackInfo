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

using href.Utils;

using System.IO;
using System.Runtime.InteropServices;

namespace iTunesLyrics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.Timer m_timer;
        //private int m_iLyricsCount;
        private DateTime m_timeStart;
        private LyricsWindow m_lrcWindow;
        public MainWindow()
        {
            InitializeComponent();
            m_timeStart = DateTime.Now;

            m_lrcWindow = new LyricsWindow();//
            //if (!m_lrcWindow.load("[DHR&Hakugetsu][Hyouka][12][720P][AVC_Hi10P_AAC][313449F5].sc - Copy.ass"))
            //if (!m_lrcWindow.load("test_lrc_jpn.lrc"))
                //MessageBox.Show("load failed!!");
            m_lrcWindow.load("test_srt_big5.srt");
            //m_lrcWindow.load("[FLsnow][Anohana][01][BDrip][AVC_FLAC+AAC][1080p].jpn.ass");
            m_lrcWindow.mergeAll();
            m_lrcWindow.rebuildLyricsUI();

            m_timer = new System.Windows.Forms.Timer();
            m_timer.Interval = 1000;
            m_timer.Tick += new EventHandler(Timer_Tick);
            m_timer.Start();
           

            //for (int i=0;i<100;i++)
            //{
            //    TextBox tbox = new TextBox();
            //    tbox.Text = i.ToString() + ". 歌詞:繁體,简体,울지마,はるなつあきふゆ";
            //    tbox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            //    Binding bnd = new Binding("Value") { ElementName = "spLyricsMain" };
            //    tbox.SetBinding(FrameworkElement.WidthProperty, bnd);
            //    tbox.TextWrapping = TextWrapping.Wrap;
            //    tbox.TextAlignment = TextAlignment.Center;
            //    spLyricsMain.Children.Add(tbox);
            //}

            //LyricsReader reader = new LyricsReader();
            ////if (reader.load("test_lrc_jpn.lrc"))//test_ass.ass
            //if (reader.load("test_ass.ass"))//
            //{
            //    if (reader.m_listLyricMain != null)
            //    {
            //        foreach (LyricsItem item in reader.m_listLyricMain)
            //        {
            //            TextBox tbox = new TextBox();
            //            //tbox.FontFamily = new FontFamily("微軟正黑體");
            //            tbox.Text = item.ToString();
            //            tbox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            //            //Binding bnd = new Binding("Value") { ElementName = "spLyricsMain" };
            //            //tbox.SetBinding(FrameworkElement.WidthProperty, bnd);
            //            tbox.TextWrapping = TextWrapping.Wrap;
            //            tbox.TextAlignment = TextAlignment.Center;
            //            spLyricsMain.Children.Add(tbox);
            //        }
            //    }
            //}

            /*

            MultiLanguage.IMultiLanguage2 multilang2 = new MultiLanguage.CMultiLanguageClass();
            if (multilang2 == null)
                throw new System.Runtime.InteropServices.COMException("Failed to get IMultilang2");

            try
            {
                //FileStream lrcStream = new FileStream("test_lrc_jpn.lrc", FileMode.Open);//test_lrc_korean.lrc
                FileStream lrcStream = new FileStream("test_lrc_korean.lrc", FileMode.Open);//test_lrc_korean.lrc

                byte[] readBuffer = new byte[4096];
                int bytesRead;
                bytesRead = lrcStream.Read(readBuffer, 0, readBuffer.Length);
                Encoding enc = EncodingTools.DetectInputCodepage(readBuffer);

                //MessageBox.Show(enc.EncodingName);
                //System.Console.WriteLine(enc.GetString(readBuffer, 0, bytesRead));
                System.Console.WriteLine(enc.EncodingName + " default : " + Encoding.Default.EncodingName);

                lrcStream.Seek(0, SeekOrigin.Begin) ;

                StreamReader lrcReader = new StreamReader(lrcStream, enc);
                Encoding enc_uni = Encoding.Unicode;

                byte[] convertedBuffer = new byte[4096];
                uint srclen = (uint)bytesRead;
                uint dstlen = 4096;
                uint nullptr = 0;
                multilang2.ConvertString(ref nullptr,(uint)enc.CodePage,(uint)enc_uni.CodePage,ref readBuffer[0],ref srclen,ref convertedBuffer[0],ref dstlen);

                //MessageBox.Show(enc_uni.GetString(convertedBuffer));

                int count = 0;
                while (!lrcReader.EndOfStream)
                {				// 每次讀取一行，直到檔尾
                    string line;
                    //if (count == 0)
                    //{
                    //    line = utf8.GetString(Encoding.Convert(enc,utf8,readBuffer,0,bytesRead));
                    //}
                    //else 
                    line = lrcReader.ReadLine();			// 讀取文字到 line 變數


                    Array.Clear(convertedBuffer, 0, convertedBuffer.Length);
                    srclen = (uint)enc.GetBytes(line).Length;
                    dstlen = 4096;
                    
                    multilang2.ConvertString(ref nullptr, (uint)enc.CodePage, (uint)enc_uni.CodePage, ref enc.GetBytes(line)[0], ref srclen, ref convertedBuffer[0], ref dstlen);
                    Array.Clear(convertedBuffer, (int)dstlen, convertedBuffer.Length - (int)dstlen);


                    //System.Console.WriteLine("srclen : " + srclen + "(" + line.Length + ") dstlen : " + dstlen + "(" + enc_uni.GetString(convertedBuffer).Length + ")");

                    //System.Console.WriteLine(enc.EncodingName + " : " + enc_uni.GetString(convertedBuffer));
                    //line = enc_uni.GetString(Encoding.Convert(enc, enc_uni, enc.GetBytes(line)));
                    //System.Console.WriteLine(enc_uni.EncodingName + " : " + line);

                    TextBox tbox = new TextBox();
                    //tbox.FontFamily = new FontFamily("微軟正黑體");
                    tbox.Text = count.ToString() + ". " + enc_uni.GetString(convertedBuffer,0,(int)dstlen);
                    tbox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    //Binding bnd = new Binding("Value") { ElementName = "spLyricsMain" };
                    //tbox.SetBinding(FrameworkElement.WidthProperty, bnd);
                    tbox.TextWrapping = TextWrapping.Wrap;
                    tbox.TextAlignment = TextAlignment.Center;
                    spLyricsMain.Children.Add(tbox);

                    count++;
                    
                    


                }

                lrcReader.Close();
                lrcStream.Close();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang2);
            }
            */

            //if (spLyricsMain.Children.Count>0)
            //{
            //    m_timer = new System.Windows.Forms.Timer();
            //    m_timer.Interval = 1000;
            //    m_timer.Tick += new EventHandler(Timer_Tick);
            //    m_timer.Start();
            //    m_iLyricsCount = 0;
            //}

            
            
            //spLyricsMain.Children.Add(new )
        }
        void Timer_Tick(object sender, EventArgs e)
        {
            //System.Console.WriteLine("scroll : " + svLyricsScroll.ContentVerticalOffset + " " + svLyricsScroll.VerticalOffset + " " + svLyricsScroll.ScrollableHeight + " " + svLyricsScroll.ViewportHeight);
            //if (m_iLyricsCount >= spLyricsMain.Children.Count)
            //    m_iLyricsCount = 0;
            //TextBox tbox = spLyricsMain.Children[m_iLyricsCount] as TextBox;
            //Point offset = tbox.TranslatePoint(new Point(0, 0), svLyricsScroll);
            //System.Console.WriteLine("lyrics : " + m_iLyricsCount + " (" + offset.X + "," + (offset.Y + svLyricsScroll.VerticalOffset) + ")  " + svLyricsScroll.VerticalOffset + " " + svLyricsScroll.ScrollableHeight);
            //m_iLyricsCount++;
            //svLyricsScroll.ScrollToVerticalOffset(Math.Max((offset.Y + svLyricsScroll.VerticalOffset) - svLyricsScroll.ViewportHeight / 2 + tbox.ActualHeight / 2, 0));
            //svLyricsScroll.Scr
            //UpdateUI();
            TimeSpan timeDis;
            timeDis = DateTime.Now - m_timeStart;
            m_lrcWindow.setSecondPositon((float)timeDis.TotalSeconds+20,1000);
            timeTextBlock.Text = timeDis.ToString() + " - " + timeDis.TotalSeconds;
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)//show
        {
            m_lrcWindow.Show();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)//hide
        {
            m_lrcWindow.Hide();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)//reset time
        {
            m_timeStart = DateTime.Now;
        }
    }
}
