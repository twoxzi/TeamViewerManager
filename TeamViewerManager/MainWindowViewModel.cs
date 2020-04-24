using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Twoxzi.TeamViewerManager.Entity;

namespace Twoxzi.TeamViewerManager
{
    /// <summary>
    /// ViewModel
    /// </summary>
    public class MainWindowViewModel : BindableBase
    {



        private CollectionView collectionView;
        private Boolean? isGroup;
        private String searchText = "";

        private ICommand deleteCommand;
        private ICommand saveCommand;
        private ICommand linkCommand;
        private ICommand ascColumnCommand;
        private ICommand addCommand;
        private ICommand outputCommand;
        private ICommand descColumnCommand;

        public Boolean? IsGroup
        {
            get { return isGroup; }
            set
            {
                isGroup = value;
                Configuration con = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if(con.AppSettings.Settings["isGroup"] == null)
                {
                    con.AppSettings.Settings.Add("isGroup", isGroup == true ? "1" : "0");
                }
                else
                {
                    con.AppSettings.Settings["isGroup"].Value = isGroup == true ? "1" : "0";
                }
                con.Save();
                GroupSwitchChanged(isGroup);
                //ConfigurationManager.AppSettings.Set("isGroup", isGroup ? "1" : "0");

            }
        }

        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value?.Trim() ?? "";
                Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { collectionView.Refresh(); }));
                OnPropertyChanged(nameof(SearchText));
            }
        }
        public ObservableCollection<TeamViewer> Collection { get; set; } = new ObservableCollection<TeamViewer>();
        public MainWindowViewModel()
        {

            var v = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(loadFiles));
            v.Completed += LoadFiles_Completed;

            isGroup = ConfigurationManager.AppSettings["isGroup"] == "1";
            collectionView = (CollectionView)CollectionViewSource.GetDefaultView(Collection);
            collectionView.Filter = x =>
            {
                TeamViewer tv = x as TeamViewer;
                if(tv == null)
                {
                    return true;
                }
                String str = SearchText.Trim();
                if(String.IsNullOrEmpty(str))
                {
                    return true;
                }
                String[] array = str.Split(' ');

                foreach(var item in array)
                {
                    if(String.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    if((tv.Id?.Contains(item) == true) || (tv.Name?.Contains(item) == true) || (tv.Memo?.Contains(item) == true) || (tv.GroupName?.Contains(item) == true))
                    {

                    }
                    else
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

        private void loadFiles()
        {
            Collection.Clear();
            foreach(TeamViewer item in TeamViewer.OpenFiles().OrderBy(x => x.Name))
            {
                Collection.Add(item);
            }
        }

        public ICommand AddCommand
        {
            get
            {
                if(addCommand == null)
                {
                    addCommand = new RelayCommand(AddCommandExecute, AddCommandCanExecuted);
                }
                return addCommand;
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        /// <param name="sender"></param>
        private void AddCommandExecute(object sender)
        {
            try
            {
                var listView = sender as ListView;
                var source = listView.ItemsSource;
                TeamViewer tv = new TeamViewer();
                // 如果由数字和空格组成,则是ID,否则是Name
                if(SearchText.All(x => x == ' ' || (x >= 48 && x <= 57)))
                {
                    tv.Id = SearchText.Trim();
                }
                else
                {
                    tv.Name = SearchText.Trim();
                }
                tv.Save();
                Collection.Add(tv);
                listView.SelectedItem = tv;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private Boolean AddCommandCanExecuted(object sender)
        {
            return !String.IsNullOrEmpty(SearchText);
        }

        /// <summary>
        /// 选中的项不为空
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Boolean SelectedItemIsNotNull(Object obj)
        {
            return (obj as ListView)?.SelectedItem != null;
        }

        /// <summary>
        /// 保存
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                if(saveCommand == null)
                {
                    saveCommand = new RelayCommand(obj =>
                                    {
                                        var listView = obj as ListView;
                                        if(listView == null)
                                        {
                                            return;
                                        }
                                        var tv = listView.SelectedItem as TeamViewer;
                                        tv?.Save();
                                    }, SelectedItemIsNotNull);
                }
                return saveCommand;
            }
        }
        /// <summary>
        /// 删除
        /// </summary>
        public ICommand DeleteCommand
        {
            get
            {
                if(deleteCommand == null)
                {
                    deleteCommand = new RelayCommand(obj =>
                      {
                          var listView = obj as ListView;
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
                      }, SelectedItemIsNotNull);
                }
                return deleteCommand;
            }
        }

        /// <summary>
        /// 连接
        /// </summary>
        public ICommand LinkCommand
        {
            get
            {
                if(linkCommand == null)
                {
                    linkCommand = new RelayCommand(obj =>
                    {
                        try
                        {
                            var listView = obj as ListView;
                            if(listView == null)
                            {
                                return;
                            }
                            TeamViewer tv = listView.SelectedItem as TeamViewer;
                            if(tv == null)
                            {
                                //MessageBox.Show("未选中连接对象，请先在列表中选择目标。");
                                return;
                            }

                            Button btn = obj as Button;
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

                            //  this.WindowState = WindowState.Minimized;
                            RefreshOrder("访问时间", false);
                            listView.SelectedItem = tv;
                            //ActiveProcess(ConfigurationManager.AppSettings["tvProcessName"]);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message + ex.StackTrace, "错误");
                        }
                    }, SelectedItemIsNotNull);
                }
                return linkCommand;
            }
        }

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
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Collection);
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new System.ComponentModel.SortDescription(pi.Name, isAsc ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending));
            }
        }
        /// <summary>
        /// 顺序
        /// </summary>
        public ICommand AscColumnCommand
        {
            get
            {
                if(ascColumnCommand == null)
                {
                    ascColumnCommand = new RelayCommand(x => SortByColumnExecute(x, true));
                }
                return ascColumnCommand;
            }
        }
        /// <summary>
        /// 倒序
        /// </summary>
        public ICommand DescColumnCommand
        {
            get
            {
                if(descColumnCommand == null)
                {
                    descColumnCommand = new RelayCommand(x => SortByColumnExecute(x, false));
                }
                return descColumnCommand;
            }
        }

        //单击表头排序
        private void SortByColumnExecute(object sender, Boolean isAsc)
        {
            if(sender is GridViewColumnHeader)
            {
                //获得点击的列  
                GridViewColumn clickedColumn = (sender as GridViewColumnHeader).Column;
                if(clickedColumn != null)
                {
                    RefreshOrder(clickedColumn.Header.ToString(), isAsc);
                }
            }
        }
        /// <summary>
        /// 分组开关
        /// </summary>
        /// <param name="IsChecked"></param>
        private void GroupSwitchChanged(Boolean? IsChecked)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(Collection);
            if(IsChecked == true)
            {
                PropertyGroupDescription groupDescription = new PropertyGroupDescription("GroupName");
                view.GroupDescriptions.Add(groupDescription);
            }
            else
            {
                view.GroupDescriptions.Clear();
            }
        }
        /// <summary>
        /// 导出
        /// </summary>
        public ICommand OutputCommand
        {
            get
            {
                if(outputCommand == null)
                {
                    outputCommand = new RelayCommand<ListView>(OutputExecute);
                }
                return outputCommand;
            }
        }

        private void OutputExecute(ListView listView)
        {
            String subDirName = "output";
            DirectoryInfo di = new DirectoryInfo(Path.Combine(TeamViewer.Folder, subDirName));
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
                fi.CopyTo(Path.Combine(di.FullName, fi.Name), true);
            }
            try
            {
                Clipboard.SetText(di.FullName);
            }
            catch { }
            MessageBox.Show($"已导出到目录{di.FullName},已将目录路径复制到粘贴板");
        }
    }
}
