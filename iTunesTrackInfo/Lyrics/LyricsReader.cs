using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using href.Utils;

using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace iTunesLyrics
{
    class LyricsItem
    {
        public float fTimeStampStart { get; set; }
        public float fTimeStampEnd { get; set; }
        public string strLyrics { get; set; }

        public LyricsItem(float start,float end,string lrcString)
        {
            fTimeStampStart = start;
            fTimeStampEnd = end;
            strLyrics = lrcString;
        }

        public override string ToString()
        {
            return string.Format("[{0:0.00} ~ {1:0.00}][{2}]", fTimeStampStart, fTimeStampEnd, strLyrics);
        }

    }

    class LyricsReader
    {
        enum LyricsType
        {
            LT_Unknown = 0,
            LT_LRC,
            LT_ASS,
            LT_SRT
        }

        public List<LyricsItem> m_listLyricMain;
        private List<List<LyricsItem>> m_listLyricContainer;

        private Regex m_regexLRC_Text = null;

        private Regex m_regexASS_Section = null;
        private Regex m_regexASS_Data = null;
        private bool m_assIsEventSection;
        private int m_assIdxTimeStart;
        private int m_assIdxTimeEnd;
        private int m_assIdxText;
        private int m_assIdxStyle;

        //private Regex m_regexSRT_Index = null;
        private Regex m_regexSRT_Time = null;
        private LyricsItem m_srtTempItem = null;
        private List<string> m_srtLyricsPool;

        private void initParser(LyricsType type)
        {
            switch (type)
            {
                case LyricsType.LT_LRC:
                    //if (m_regexLRC == null)
                        //m_regexLRC = new Regex(@"^(\[(\d+):(\d+)\.(\d+)\])+(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
                    if (m_regexLRC_Text == null)
                        m_regexLRC_Text = new Regex(@"^(\[.*\])(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);

                    break;

                case LyricsType.LT_ASS:
                    if (m_regexASS_Section == null)
                        m_regexASS_Section = new Regex(@"^\[(.*?)\]$", RegexOptions.Compiled | RegexOptions.Singleline);
                    if (m_regexASS_Data == null)
                        m_regexASS_Data = new Regex(@"^([A-Za-z0-9]+?):(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
                        //m_regexASS_Data = new Regex(@"^(\w+):\s*(\w*?,)?(\w*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
                    m_assIsEventSection = false;
                    m_assIdxTimeStart = -1;
                    m_assIdxTimeEnd = -1;
                    m_assIdxText = -1;
                    m_assIdxStyle = -1;
                    break;

                case LyricsType.LT_SRT:
                    //if (m_regexSRT_Index == null)
                    //    m_regexSRT_Index = new Regex(@"^\d$", RegexOptions.Compiled | RegexOptions.Singleline);
                    if (m_regexSRT_Time == null)
                        m_regexSRT_Time = new Regex(@"^(.*?)-->(.*?)$", RegexOptions.Compiled | RegexOptions.Singleline);
                    if (m_srtLyricsPool == null)
                        m_srtLyricsPool = new List<string>();
                    m_srtTempItem = null;
                    m_srtLyricsPool.Clear();
                    break;



            }
        }

        private void addString2LyricsItem(List<LyricsItem>list,LyricsType type,string line)
        {
            LyricsItem res = null;

            try{
                string[] matches;
                switch (type)
                {
                    case LyricsType.LT_LRC:
                        matches = m_regexLRC_Text.Split(line);
                        float timeStart = 0;
                        if (matches.Length==4)
                        {
                            string[] times = matches[1].Split(':','.','[',']');
                            for (int t = 0; t < times.Length;t++ )
                            {
                                switch (t%4)
                                {
                                    case 1:
                                        timeStart = float.Parse(times[t]) * 60;
                                        break;
                                    case 2:
                                        timeStart += float.Parse(times[t]);
                                        break;
                                    case 3:
                                        timeStart += float.Parse(times[t]) / 100.0f;
                                        res = new LyricsItem(timeStart, -1, matches[2]);
                                        list.Add(res);
                                        break;
                                    default:
                                        break;
                                }
                            }
                                //res = new LyricsItem(float.Parse(matches[2]) * 60 + float.Parse(matches[3]) + float.Parse(matches[4]) / 100.0f, -1, matches[5]);
                        }
                        break;

                    case LyricsType.LT_ASS:
                        if (!m_assIsEventSection)
                        {
                            //Console.WriteLine("IsMatch: " + m_regexASS_Section.IsMatch(line));
                            if (m_regexASS_Section.IsMatch(line))
                            {
                                matches = m_regexASS_Section.Split(line);
                                if (matches.Length == 3 && matches[1].Equals("Events"))
                                {
                                    m_assIsEventSection = true;
                                    //Console.WriteLine("m_assIsEventSection = true!!!");
                                }
                                for (int i = 0; i < matches.Length; i++)
                                {
                                    //Console.WriteLine("   " + i + "/" + matches.Length + "[" + matches[i] + "]");
                                }
                            }
                        }
                        else
                        {
                            matches = m_regexASS_Data.Split(line);
                            if (matches.Length==4)
                            {
                                matches[2] = Regex.Replace(matches[2], @"{.*?}", string.Empty);
                                string[] label = matches[2].Split(',');

                                string labelHeader = Regex.Replace(matches[1], @"\s+", string.Empty);
                                if (labelHeader.Equals("Format"))
                                {
                                    for (int i = 0; i < label.Length; i++)
                                    {
                                        label[i] = Regex.Replace(label[i], @"\s+", string.Empty);
                                        if (label[i].Equals("Start"))
                                            m_assIdxTimeStart = i;
                                        else if (label[i].Equals("End"))
                                            m_assIdxTimeEnd = i;
                                        else if (label[i].Equals("Text"))
                                            m_assIdxText = i;
                                        else if (label[i].Equals("Style")) 
                                            m_assIdxStyle = i;
                                        //Console.WriteLine("   Lable:" + i + "/" + label.Length + "[" + label[i] + "]");
                                    }
                                }
                                else if (labelHeader.Equals("Dialogue") && m_assIdxTimeStart != -1 && m_assIdxText != -1)
                                {
                                    bool isSkip = false;
                                    for (int i = 0; i < label.Length; i++)
                                    {
                                        //label[i] = Regex.Replace(label[i], @"\s+", string.Empty);
                                        //Console.WriteLine("   Lable:" + i + "/" + label.Length + "[" + label[i] + "]");
                                    }
                                    string[] timesStart;
                                    string[] timesEnd = null;

                                    label[m_assIdxTimeStart] = Regex.Replace(label[m_assIdxTimeStart], @"\s+", string.Empty);
                                    timesStart = label[m_assIdxTimeStart].Split(':', '.');
                                    if (m_assIdxTimeEnd != -1)
                                    {
                                        label[m_assIdxTimeEnd] = Regex.Replace(label[m_assIdxTimeEnd], @"\s+", string.Empty);
                                        timesEnd = label[m_assIdxTimeEnd].Split(':', '.');
                                    }

                                    if (m_assIdxStyle != -1)
                                    {
                                        if (label[m_assIdxStyle].Equals("ed") || label[m_assIdxStyle].Equals("op"))
                                        {
                                            isSkip = true;
                                            //Console.WriteLine("   Name:" + label[m_assIdxStyle] + "  " + label[m_assIdxText]);    
                                        }
                                        
                                    }

                                    if (timesStart.Length == 4 && !isSkip)
                                    {
                                        label[m_assIdxText] = label[m_assIdxText].Replace("\\n", Environment.NewLine);
                                        label[m_assIdxText] = label[m_assIdxText].Replace("\\N", Environment.NewLine);
                                        label[m_assIdxText] = label[m_assIdxText].Replace("\\h", string.Empty);
                                        res = new LyricsItem(float.Parse(timesStart[0]) * 3600 + float.Parse(timesStart[1]) * 60 + float.Parse(timesStart[2]) + float.Parse(timesStart[3]) / 100,
                                            (m_assIdxTimeEnd != -1) ? float.Parse(timesEnd[0]) * 3600 + float.Parse(timesEnd[1]) * 60 + float.Parse(timesEnd[2]) + float.Parse(timesEnd[3]) / 100 : -1,
                                            label[m_assIdxText]);
                                        list.Add(res);
                                    }
                                }
                                else 
                                {
                                    for (int i = 0; i < label.Length; i++)
                                    {
                                        label[i] = Regex.Replace(label[i], @"\s+", string.Empty);
                                        //Console.WriteLine("   Error!!!! Lable:" + i + "/" + label.Length + "[" + label[i] + "]");
                                    }
                                }
                                

                            }
                            for (int i = 0; i < matches.Length; i++)
                            {
                                //Console.WriteLine("   " + i + "/" + matches.Length + "[" + matches[i] + "]");
                            }
                        }
                        break;


                    case LyricsType.LT_SRT:


                         
                        if (m_regexSRT_Time.IsMatch(line))
                        {
                            matches = m_regexSRT_Time.Split(line);

                            matches[1] = Regex.Replace(matches[1], @"\s+", string.Empty);
                            matches[2] = Regex.Replace(matches[2], @"\s+", string.Empty);
                            string[] timeStrStart = matches[1].Split(':', ',');
                            string[] timeStrEnd = matches[2].Split(':', ',');

                            if (m_srtTempItem!=null)
                            {
                                for (int l=0;l<m_srtLyricsPool.Count-1;l++)
                                {
                                    if (l == 0)
                                        m_srtTempItem.strLyrics = m_srtLyricsPool[l];
                                    else
                                        m_srtTempItem.strLyrics += Environment.NewLine + m_srtLyricsPool[l];
                                }
                                list.Add(m_srtTempItem);
                                m_srtTempItem = null;
                            }

                            m_srtTempItem = new LyricsItem(float.Parse(timeStrStart[0]) * 3600 + float.Parse(timeStrStart[1]) * 60 + float.Parse(timeStrStart[2]) + float.Parse(timeStrStart[3]) / 1000,
                                            float.Parse(timeStrEnd[0]) * 3600 + float.Parse(timeStrEnd[1]) * 60 + float.Parse(timeStrEnd[2]) + float.Parse(timeStrEnd[3]) / 1000,
                                            string.Empty);
                            m_srtLyricsPool.Clear();
                        }
                        else
                        {
                            string emtpyLine = Regex.Replace(line, @"\s+", string.Empty);
                            if (emtpyLine.Length != 0)
                            {
                                string lineClean;
                                lineClean = Regex.Replace(line, @"{.*?}", string.Empty);
                                lineClean = Regex.Replace(lineClean, @"<.*?>", string.Empty);
                                m_srtLyricsPool.Add(lineClean);
                            }
                        }
                        break;

                }
            }
            catch (Exception)
            {
                res = null;
            }

                

        }


        public void clear()
        {
            if (m_listLyricContainer!=null)
            {
                foreach (List<LyricsItem> list in m_listLyricContainer)
                    list.Clear();
                m_listLyricContainer = null;
            }
            if (m_listLyricMain != null)
                m_listLyricMain = null;
        }

        public bool load(string path)
        {
            bool res = true;

            string fileExt = System.IO.Path.GetExtension(path);
            fileExt.ToLower();

            LyricsType lType = LyricsType.LT_Unknown;

            if (fileExt.Equals(".lrc"))
                lType = LyricsType.LT_LRC;
            if (fileExt.Equals(".ass"))
                lType = LyricsType.LT_ASS;
            if (fileExt.Equals(".srt"))
                lType = LyricsType.LT_SRT;

            if (lType == LyricsType.LT_Unknown)
                return false;

            initParser(lType);

            MultiLanguage.IMultiLanguage2 multilang2 = new MultiLanguage.CMultiLanguageClass();
            if (multilang2 == null)
                throw new System.Runtime.InteropServices.COMException("Failed to get IMultilang2");

            try
            {
                List<LyricsItem> lrcList = new List<LyricsItem>();
                FileStream lrcStream = new FileStream(path, FileMode.Open);//test_lrc_korean.lrc

                byte[] readBuffer = new byte[4096];
                int bytesRead;
                bytesRead = lrcStream.Read(readBuffer, 0, readBuffer.Length);
                Encoding enc = EncodingTools.DetectInputCodepage(readBuffer);

                System.Console.WriteLine(enc.EncodingName + " default : " + Encoding.Default.EncodingName);

                lrcStream.Seek(0, SeekOrigin.Begin);

                StreamReader lrcReader = new StreamReader(lrcStream, enc);
                Encoding enc_uni = Encoding.Unicode;

                //byte[] convertedBuffer = new byte[4096];
                uint srclen = (uint)bytesRead;
                uint dstlen = 4096;
                uint nullptr = 0;
                //multilang2.ConvertString(ref nullptr, (uint)enc.CodePage, (uint)enc_uni.CodePage, ref readBuffer[0], ref srclen, ref convertedBuffer[0], ref dstlen);

                //MessageBox.Show(enc_uni.GetString(convertedBuffer));

                int count = 0;
                while (!lrcReader.EndOfStream)
                {				
                    string line;
                    line = lrcReader.ReadLine();

                    if (line.Length == 0)
                        continue;

                    Array.Clear(readBuffer, 0, readBuffer.Length);
                    srclen = (uint)enc.GetBytes(line).Length;
                    dstlen = (uint)readBuffer.Length;

                    multilang2.ConvertString(ref nullptr, (uint)enc.CodePage, (uint)enc_uni.CodePage, ref enc.GetBytes(line)[0], ref srclen, ref readBuffer[0], ref dstlen);
                    line = enc_uni.GetString(readBuffer, 0, (int)dstlen);

                    addString2LyricsItem(lrcList, lType, line);
                    count++;
                }

                if (lType == LyricsType.LT_SRT && m_srtTempItem!=null)
                {
                    for (int l = 0; l < m_srtLyricsPool.Count; l++)
                    {
                        if (l == 0)
                            m_srtTempItem.strLyrics = m_srtLyricsPool[l];
                        else
                            m_srtTempItem.strLyrics += Environment.NewLine + m_srtLyricsPool[l];
                    }
                    lrcList.Add(m_srtTempItem);
                    m_srtTempItem = null;
                }

                lrcReader.Close();
                lrcStream.Close();

                sortList(lrcList);

                if (m_listLyricContainer == null)
                    m_listLyricContainer = new List<List<LyricsItem>>();
                m_listLyricContainer.Add(lrcList);
                m_listLyricMain = lrcList;


            }
            catch (Exception e)
            {
                //Console.WriteLine("LyricsReader : " + e.ToString());
                //System.Windows.Forms.MessageBox.Show(e.ToString());
                res = false;
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang2);
            }

            return res;

        }

        private void sortList(List<LyricsItem> lrcList)
        {
            lrcList.Sort((x, y) => { return x.fTimeStampStart.CompareTo(y.fTimeStampStart); });
            for (int i = 0; i < lrcList.Count - 1; i++)
            {
                if (lrcList[i].fTimeStampEnd == -1)
                    lrcList[i].fTimeStampEnd = lrcList[i + 1].fTimeStampStart;
                else if (lrcList[i + 1].fTimeStampStart < lrcList[i].fTimeStampEnd)
                {
                    lrcList[i].strLyrics += Environment.NewLine + lrcList[i + 1].strLyrics;
                    if (lrcList[i].fTimeStampEnd < lrcList[i + 1].fTimeStampEnd)
                        lrcList[i].fTimeStampEnd = lrcList[i + 1].fTimeStampEnd;
                    lrcList.RemoveAt(i + 1);
                    i--;
                }

            }
        }

        public bool mergeAll()
        {
            if (m_listLyricContainer == null || m_listLyricContainer.Count == 1) return false;
            List<LyricsItem> lrcList = new List<LyricsItem>();
            foreach (List<LyricsItem> list in m_listLyricContainer)
            {
                foreach (LyricsItem item in list)
                {
                    lrcList.Add(item);
                }
            }
            sortList(lrcList);
            m_listLyricMain = lrcList;
            return true;
        }





        //students.Sort((x, y) => { return -x.Score.CompareTo(y.Score); });
        //最後會傳回 1、0 或 –1
        //代表的意思分別為：x > y, x == y, x < y。
    }
}
