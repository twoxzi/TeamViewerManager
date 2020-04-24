using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace Twoxzi.TeamViewerManager.Entity
{
    public enum TeamViewerAction
    {
        /// <summary>
        /// 远程文件
        /// </summary>
        Filetransfer = 1,
        /// <summary>
        /// 远程桌面
        /// </summary>
        RemoteSupport = 0
    }

    public class TeamViewer : BindableBase
    {
        #region Fields
        private String id;
        private String password;
        private String name;
        private String memo;
        private String filePath;
        private TeamViewerAction action;
        private static String folder;
        private String groupName;
        private String lastTime;
        #endregion Fields

        #region Properties
        [Description("action")]
        [TeamviewerKeyAttribute("action")]
        public TeamViewerAction Action
        {
            get { return action; }
            set
            {
                action = value;
                OnPropertyChanged("Action");
            }
        }
        /// <summary>
        /// TeamViewer Id
        /// </summary>
        /// 
        [Description("ID")]
        [TeamviewerKeyAttribute("targetId")]
        public String Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
                OnPropertyChanged("Id");
            }
        }
        /// <summary>
        /// 密码
        /// </summary>
        /// 
        [TeamviewerKeyAttribute("password")]
        [Description("密码")]
        public string Password
        {
            get
            {
                return password;
            }

            set
            {
                password = value;
                OnPropertyChanged("Password");
            }
        }
        /// <summary>
        /// 名称
        /// </summary>
        /// 
        [TeamviewerKeyAttribute("name")]
        [Description("名称")]
        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        /// 
        [TeamviewerKeyAttribute("memo")]
        [Description("备注")]
        public string Memo
        {
            get
            {
                return memo;
            }

            set
            {
                memo = value;
                OnPropertyChanged("Memo");
            }
        }
        [TeamviewerKeyAttribute("groupName")]
        [Description("分组名")]
        public String GroupName
        {
            get { return groupName; }
            set { groupName = value; OnPropertyChanged(nameof(GroupName)); }
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get
            {
                if(filePath == null)
                {
                    filePath = Path.Combine(Folder, DateTime.Now.ToString("yyyyMMddHHmmss") + ".tvc");
                }
                return filePath;
            }

            set
            {
                filePath = value;
                OnPropertyChanged("FilePath");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [TeamviewerKeyAttribute(nameof(LastTime))]
        [Description("访问时间")]
        public String LastTime
        {
            get
            {
                return lastTime;
            }

            set
            {
                this.lastTime = value;
                OnPropertyChanged(nameof(LastTime));
            }
        }
        public static string Folder
        {
            get
            {
                if(folder == null)
                {
                    folder = ConfigurationManager.AppSettings["folder"];
                    if(folder == null)
                    {
#if DEBUG
                        folder = "D:\\temp\\tvcFiles";
#else
                        folder = "tvcFiles";
#endif
                    }
                }
                return folder;
            }
        }



#endregion Properties

        public static Dictionary<String,PropertyInfo> PropertyDescriptionDic { get; set; }

        static TeamViewer()
        {
            PropertyDescriptionDic = new Dictionary<string, PropertyInfo>();
            foreach(PropertyInfo property in typeof(TeamViewer).GetProperties())
            {
                String key = (Attribute.GetCustomAttribute(property, typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description;
                if(!String.IsNullOrEmpty(key))
                {
                    if(!PropertyDescriptionDic.ContainsKey(key))
                    {
                        PropertyDescriptionDic.Add(key, property);
                    }
                }
            }
        }

        public static TeamViewer OpenFile(String filePath)
        {
            TeamViewer tv = new TeamViewer();
            tv.Open(filePath);
            return tv;
        }

        public static List<TeamViewer> OpenFiles()
        {
            List<TeamViewer> collection = new List<TeamViewer>();
            if(!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }
            foreach(String item in Directory.GetFiles(Folder))
            {
                collection.Add(TeamViewer.OpenFile(item));
            }
            return collection;
        }

        public void Open(String filePath)
        {
            PropertyInfo[] pis = this.GetType().GetProperties();
            foreach(PropertyInfo property in pis)
            {
                String key = (Attribute.GetCustomAttribute(property, typeof(TeamviewerKeyAttribute)) as TeamviewerKeyAttribute)?.KeyName;
                if(key != null && key.Length > 0)
                {
                    String value = OperateIniFile.ReadIniData("TeamViewer Configuration", key, "", filePath);
                    if(value != null && value.Length > 0)
                    {
                        if(property.PropertyType.IsEnum) //属性类型是否表示枚举
                        {

                            object enumName = Enum.Parse(property.PropertyType, value);
                            property.SetValue(this, enumName, null); //获取枚举值，设置属性值
                        }
                        else
                        {
                            Object obj = value;
                            if(property.PropertyType.IsValueType && String.IsNullOrEmpty(value))
                            {
                                obj = Activator.CreateInstance(property.PropertyType);
                            }
                            property.SetValue(this, obj, null);
                        }
                    }
                }

            }
            FilePath = filePath;

        }

        public Boolean Save(String filePath = null)
        {
            this.LastTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if(filePath == null)
            {
                if(this.FilePath == null)
                {
                    throw new Exception("文件路径不合法");
                }
                else
                {
                    filePath = FilePath;
                }
            }
            if(!File.Exists(filePath))
            {
                using(File.CreateText(filePath)) { }
            }
            PropertyInfo[] pis = this.GetType().GetProperties();
            foreach(PropertyInfo item in pis)
            {
                String key = (Attribute.GetCustomAttribute(item, typeof(TeamviewerKeyAttribute)) as TeamviewerKeyAttribute)?.KeyName;
                if(key != null && key.Length > 0)
                {
                    String value = item.GetValue(this, null)?.ToString();
                    OperateIniFile.WriteIniData("TeamViewer Configuration", key, value, filePath);
                    //if (value != null && value.Length > 0)
                    //{
                    //    item.SetValue(this, value, null);
                    //}
                }

            }
            return true;
        }
    }
}
