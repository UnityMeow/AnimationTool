using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace AnimationTool
{
    class CMove
    {
        //图片文件路径
        string m_Path;
        //图片名
        string[] m_PicName;
        //图片数量
        int m_Length;
        //图片数据
        Dictionary<string, BitmapImage> m_PicList;

        //图片帧率
        int m_Fps;
        //动作名
        string m_MoveName;

        public CMove(string path)
        {
            m_Path = path;
            m_MoveName = "未命名";
            m_PicList = new Dictionary<string, BitmapImage>();
            m_Fps = 0;
            m_Length = 0;
            LoadPic();
        }
        //获取图片长度
        public int GetPicLength()
        {
            return m_Length;
        }
        //获取图片数据
        public BitmapImage GetPicData(int index)
        {
            if (!m_PicList.ContainsKey(m_PicName[index]))
                return m_PicList[m_PicName[0]];
            return m_PicList[m_PicName[index]];
        }
        //获取动作名
        public string GetMoveName()
        {
            return m_MoveName;
        }
        //获取图片帧率
        public int GetFPS()
        {
            return m_Fps;
        }
        //加载图片
        void LoadPic()
        {
            //加载指定路径文件信息
            DirectoryInfo Dinfo = new DirectoryInfo(m_Path);
            //得到对应目录下所有文件
            FileInfo[] Finfo = Dinfo.GetFiles();
            m_PicName = new string[Dinfo.GetFiles().Length];
            //筛选图片文件存入表
            for (int i = 0; i < Dinfo.GetFiles().Length; i++)
            {
                if (Finfo[i].Extension == ".png")
                {
                    BitmapImage bTmp = new BitmapImage(new Uri(Finfo[i].FullName));
                    m_PicList.Add(Finfo[i].Name, bTmp);
                    m_PicName[m_Length++] = Finfo[i].Name;
                }
            }
        }
        //设置动作名
        public void SetMoveName(string name)
        {
            if (name == "")
                m_MoveName = "未命名";
            m_MoveName = name;
        }
        //设置图片帧率
        public void SetFPS(int fps)
        {
            m_Fps = fps;
        }

    }
}
