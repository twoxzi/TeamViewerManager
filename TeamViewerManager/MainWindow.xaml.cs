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

namespace TeamViewerManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 激活外部窗口
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="fAltTab"></param>
        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        ObservableCollection<TeamViewer> Collection = new ObservableCollection<TeamViewer>();
        Dictionary<GridViewColumn, Boolean> ListViewOrderAsc = new Dictionary<GridViewColumn, Boolean>();
        public MainWindow()
        {
            InitializeComponent();
           var v= this.Dispatcher.BeginInvoke(new Action(loadFiles));
            v.Completed += LoadFiles_Completed;
            
            listView.ItemsSource = Collection;
            // Console.WriteLine("分组");
            // 分组显示
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(listView.ItemsSource);
            PropertyGroupDescription groupDescription = new PropertyGroupDescription("GroupName");
            view.GroupDescriptions.Add(groupDescription);


            



            loadListViewColumns();
        }

        private void LoadFiles_Completed(Object sender, EventArgs e)
        {
            // 加载完成后排序
            RefreshOrder(null, true);
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
                RefreshOrder(null, true);
                ActiveProcess(ConfigurationManager.AppSettings["tvProcessName"]);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace, "错误");
            }
        }

        private void ActiveProcess(String pName)
        {
            if(pName != null && pName.Length > 0)
            {
                Process[] temp = Process.GetProcessesByName(pName);
                if(temp.Length > 0)//如果查找到
                {
                    IntPtr handle = temp[0].MainWindowHandle;
                    if(handle != IntPtr.Zero)
                    {
                        // 激活，显示在最前
                        SwitchToThisWindow(handle, true);
                    }
                }
                else
                {
                    MessageBox.Show("没有找到" + pName);
                }
            }
        }

        //private Func<TeamViewer, String> OrderFunc;

        /// <summary>
        /// 刷新排序
        /// </summary>
        /// <param name="func"></param>
        /// <param name="isAsc"></param>
        private void RefreshOrder(Func<TeamViewer, String> func, Boolean isAsc)
        {
            IOrderedEnumerable<TeamViewer> order = Collection.OrderByDescending(x => x.LastTime);
            if(func != null)
            {
                order = isAsc ? order.ThenBy(func) : order.ThenByDescending(func);
            }
            List<TeamViewer> list = new List<TeamViewer>(order);
            Collection.Clear();
            foreach(var item in list)
            {
                Collection.Add(item);
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


                    Func<TeamViewer, String> OrderFunc = null;
                    switch(clickedColumn.Header.ToString())
                    {
                        case "ID": OrderFunc = (x) => x.Id; break;
                        case "名称": OrderFunc = x => x.Name; break;
                        case "密码": OrderFunc = x => x.Password; break;
                        case "备注": OrderFunc = x => x.Memo; break;
                    }


                    RefreshOrder(OrderFunc, isAsc);
                }
            }
        }
    }
}
