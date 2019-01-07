using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Twoxzi.TeamViewerManager.Entity;

using System.Runtime.InteropServices;
using System.Configuration;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Data;
using System.Reflection;

namespace TeamViewerManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ///// <summary>
        ///// 激活外部窗口
        ///// </summary>
        ///// <param name="hWnd"></param>
        ///// <param name="fAltTab"></param>
        //[DllImport("user32.dll")]
        //public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        ObservableCollection<TeamViewer> Collection = new ObservableCollection<TeamViewer>();
        Dictionary<GridViewColumn, Boolean> ListViewOrderAsc = new Dictionary<GridViewColumn, Boolean>();

        CollectionView collectionView;

        private Boolean isGroup;
        public Boolean IsGroup
        {
            get { return isGroup; }
            set
            {
                isGroup = value;
                Configuration con = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if(con.AppSettings.Settings["isGroup"] == null)
                {
                    con.AppSettings.Settings.Add("isGroup", isGroup ? "1" : "0");
                }
                else
                {
                    con.AppSettings.Settings["isGroup"].Value = isGroup ? "1" : "0";
                }
                con.Save();
                
                //ConfigurationManager.AppSettings.Set("isGroup", isGroup ? "1" : "0");
                
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            var v = this.Dispatcher.BeginInvoke(new Action(loadFiles));
            v.Completed += LoadFiles_Completed;
            listView.ItemsSource = Collection;
            isGroup = ConfigurationManager.AppSettings["isGroup"] == "1";
            

            loadListViewColumns();
            collectionView = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            collectionView.Filter = x =>
            {
                TeamViewer tv = x as TeamViewer;
                if(tv == null)
                {
                    return true;
                }
                String str = txtSearch.Text.Trim();
                if(String.IsNullOrEmpty(str))
                {
                    return true;
                }
                String[] array = str.Split(' ');
                
                //var ps = TeamViewer.PropertyDescriptionDic.Values.ToList();
                foreach(var item in array)
                {
                    if(String.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    if((tv.Id?.Contains(item) == true) || (tv.Name?.Contains(item) == true) || (tv.Memo?.Contains(item) == true) || (tv.GroupName?.Contains(item) == true))
                    {

                    }else
                    {
                        return false;
                    }
                }
                return true;
            };
        }

        private void LoadFiles_Completed(Object sender, EventArgs e)
        {
            // 加载完成后排序
            RefreshOrder("访问时间", false);
        }

        private void loadListViewColumns()
        {
            GridView gv = listView.View as GridView;
            if(gv != null)
            {
                foreach(var item in gv.Columns)
                {
                    ListViewOrderAsc.Add(item, true);
                }
            }
        }

        private void loadFiles()
        {
            Collection.Clear();
            foreach(TeamViewer item in TeamViewer.OpenFiles().OrderBy(x => x.Name))
            {
                Collection.Add(item);
            }
        }

        private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Button_Click_3(sender, e);
        }

        // 新增
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TeamViewer tv = new TeamViewer();
                tv.Save();
                Collection.Add(tv);
                listView.SelectedItem = tv;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // save
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            TeamViewer tv = listView.SelectedItem as TeamViewer;
            tv?.Save();
        }


        // 删除
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            TeamViewer tv = listView.SelectedItem as TeamViewer;
            if(tv == null)
            {
                return;
            }
            if(tv.FilePath != null && tv.FilePath.Length > 0)
            {
                File.Delete(tv.FilePath);
            }
            Collection.Remove(tv);
            listView.SelectedIndex = Collection.Count - 1;
        }

        // 连接
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            try
            {
                TeamViewer tv = listView.SelectedItem as TeamViewer;
                if(tv == null)
                {
                    //MessageBox.Show("未选中连接对象，请先在列表中选择目标。");
                    return;
                }

                Button btn = sender as Button;
                if(btn != null)
                {
                    tv.Action = btn.Content?.ToString() == "文件" ? TeamViewerAction.Filetransfer : TeamViewerAction.RemoteSupport;
                }

                tv.Save();

                if(tv.Password != null && tv.Password.Length > 0)
                {
                    Clipboard.SetText(tv.Password);
                }
                Process.Start(tv.FilePath);
                this.WindowState = WindowState.Minimized;
                RefreshOrder("访问时间", false);
                listView.SelectedItem = tv;
                //ActiveProcess(ConfigurationManager.AppSettings["tvProcessName"]);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "错误");
            }
        }

        //private void ActiveProcess(String pName)
        //{
        //    if(pName != null && pName.Length > 0)
        //    {
        //        Process[] temp = Process.GetProcessesByName(pName);
        //        if(temp.Length > 0)//如果查找到
        //        {
        //            IntPtr handle = temp[0].MainWindowHandle;
        //            if(handle != IntPtr.Zero)
        //            {
        //                // 激活，显示在最前
        //                SwitchToThisWindow(handle, true);
        //            }
        //        }
        //        else
        //        {
        //            MessageBox.Show("没有找到" + pName);
        //        }
        //    }
        //}
        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="header"></param>
        /// <param name="isAsc"></param>
        private void RefreshOrder(String header, Boolean isAsc)
        {
            PropertyInfo pi;

            if(TeamViewer.PropertyDescriptionDic.TryGetValue(header, out pi))
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
                while(view.SortDescriptions.Count > 0)
                {
                    view.SortDescriptions.RemoveAt(0);
                }
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription(pi.Name, isAsc ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending));
            }
        }

        //单击表头排序  
        private void ButtonSort_Click(object sender, RoutedEventArgs e)
        {
            if(e.OriginalSource is GridViewColumnHeader)
            {
                //获得点击的列  
                GridViewColumn clickedColumn = (e.OriginalSource as GridViewColumnHeader).Column;
                if(clickedColumn != null)
                {
                    Boolean isAsc = ListViewOrderAsc[clickedColumn];
                    ListViewOrderAsc[clickedColumn] = !isAsc;
                    RefreshOrder(clickedColumn.Header.ToString(), isAsc);
                }
            }
        }

        private void cbxIsGroup_Checked(Object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("GroupName");
            view.GroupDescriptions.Add(groupDescription);
        }

        private void cbxIsGroup_Unchecked(Object sender, RoutedEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            while(view.GroupDescriptions.Count > 0)
            {
                view.GroupDescriptions.RemoveAt(0);
            }
        }

        private void btnSearch_Click(Object sender, RoutedEventArgs e)
        {
            collectionView.Refresh();
        }

        private void txtSearch_TextChanged(Object sender, TextChangedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { collectionView.Refresh(); }));
        }

        private void btnOutput_Click(Object sender, RoutedEventArgs e)
        {
            String subDirName = "output";
            DirectoryInfo di = new DirectoryInfo(Path.Combine( TeamViewer.Folder,subDirName));
            if(di.Exists)
            {
                di.Delete(true);
            }
            di.Create();
            if(listView.SelectedItems.Count == 0)
            {

                return;
            }
            var list = listView.SelectedItems.OfType<TeamViewer>();
            foreach(var item in list)
            {
                FileInfo fi = new FileInfo(item.FilePath);
                fi.CopyTo(Path.Combine(di.FullName, fi.Name),true);
            }
            try
            {
                Clipboard.SetText(di.FullName);
            }
            catch { }
            MessageBox.Show($"已导出到目录{di.FullName}");

        }
    }
}
