using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Twoxzi.TeamViewerManager.Entity
{
    /// <summary>
    /// 基础类
    /// </summary>
    public class BindableBase : INotifyPropertyChanged
    {


        private Dictionary<String, String> dict = new Dictionary<String, String>();

        // 只有debug下有效，不要也罢。
        //private static string GetProperyName(string methodName)
        //{
        //    if(methodName.StartsWith("get_") || methodName.StartsWith("set_") || methodName.StartsWith("put_"))
        //    {
        //        return methodName.Substring("get_".Length);
        //    }
        //    throw new Exception(methodName + " not a method of Property");
        //}
        /// <summary>
        /// 属性改变
        /// </summary>
        /// <param name="propertyName"></param>
        public void OnPropertyChanged(String propertyName = null)
        {
            //if(propertyName == null)
            //{
            //    propertyName = GetProperyName(new StackTrace(true).GetFrame(1).GetMethod().Name);
            //}
            //lock(dict)
            //{
            //    if(dict.ContainsKey(propertyName))
            //    {
            //        propertyName = dict[propertyName];
            //    }
            //    else
            //    {
            //        String prePropertyName = propertyName;
            //        propertyName = GetProperyName(prePropertyName);
            //        dict.Add(prePropertyName, propertyName);
            //    }
            //}
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

        /// <summary>
        /// 属性改变
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
