using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterCLS
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Profile : Page
    {
        public ApplicationDataContainer Set = ApplicationData.Current.LocalSettings;
        public CommentCsvParser parser;
        public GpaJsonParser gpa_parser;
        public ProfileParser profile_parser;
        public Profile()
        {
            InitializeComponent();
            InitializeFolder();
            
        }
        private void InitializeFolder()
        {
            string target = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "database");
            string path = ValidationHelper.GetToken(Set, "Path");
            if (path != "0")
            {
                target = path;
            }
            parser = new CommentCsvParser(target);
            gpa_parser = new GpaJsonParser(target);
            profile_parser = new ProfileParser(target);
        }
        protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传递的参数
            var parameter = e.Parameter as string;

            if (parameter != null)
            {
                LoadProfile(parameter);
            }
            
        }
        private void LoadProfile(string id)
        {
            

            // 创建解析器实例
            
            var list=parser.GetCommentsByTeacherId(id);
            CommentList.ItemsSource= list;
            var profile = profile_parser.FindTeacherById(Convert.ToInt32(id));
            ProfileGrid.DataContext= profile;
            NamePresenter.DataContext= profile;
            var gpa_list = gpa_parser.GetCoursesByTeacher(profile.Name);
            GpaList.ItemsSource= gpa_list;
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void settings_Click(object sender, RoutedEventArgs e)
        {
            source.Text = ValidationHelper.GetToken(Set, "Path")=="0"?"内置数据": ValidationHelper.GetToken(Set, "Path");
            About.IsOpen = true;
        }

        private void GpaList_ItemClick(object sender, ItemClickEventArgs e)
        {
            TeacherList.ItemsSource = null;
            var c = e.ClickedItem as CourseGpaInfo;
            if (c != null)
            {
                var list = gpa_parser.GetTeachersByCourse(c.CourseName);
                TeacherList.ItemsSource = list;
            }
        }

        private void TeacherList_ItemClick(object sender, ItemClickEventArgs e)
        {
            TeacherList.ItemsSource = null;
            var c = e.ClickedItem as CourseGpaInfo;
            if (c != null)
            {
                var n = c.TeacherName;
                var t = profile_parser.FindTeachersByName(n).FirstOrDefault();
                if(t != null)
                {
                    Frame.Navigate(typeof(Profile), t.Id.ToString());
                }
            }
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            About.IsOpen = false;
        }

        private async void Git_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/Ginsenvey/BetterCLS/"));
        }

        private async void source_Click(object sender, RoutedEventArgs e)
        {
            var b = sender as Button;
            b.IsEnabled = false;
            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();
            var window = (App.Current as App)._window;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                try
                {
                    var parser = new ProfileParser(folder.Path);
                    var profile = parser.FindTeacherById(1);
                    if (profile != null)
                    {
                        Set.Values["Path"] = folder.Path;
                        Flower.PlayAnimation("\uE930", "更新数据源成功");
                    }
                    else
                    {
                        Flower.PlayAnimation("\uEA39", "数据源为空");
                    }
                }
                catch
                {
                    Flower.PlayAnimation("\uEA39", "数据源不正确");
                }
            }
            b.IsEnabled = true;
        }

        private void rec_Click(object sender, RoutedEventArgs e)
        {
            Set.Values["Path"] = "0";
            Flower.PlayAnimation("\uE930", "重启应用生效");
        }
    }
}
