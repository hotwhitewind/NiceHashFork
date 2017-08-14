using System;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Win32;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

namespace WixSharp_Setup1
{
    class Program
    {
        static void Main()
        {
            Dialog productActivationDialog = new AdditionalOptionsDlg().ToWDialog();
            var project = new Project("bCash",
                new Dir(@"%ProgramFiles%\bCash",
                    new File("..\\Release\\AMDOpenCLDeviceDetection.exe"),
                    new File("..\\Release\\bCash.exe",
                        new FileShortcut("bCash", @"%Desktop%")
                        {
                            IconFile = @"..\..\AppFiles\icon_.ico"
                        }),
                    new ExeFileShortcut("Uninstall bCash", "[System64Folder]msiexec.exe", "/x [ProductCode]")
                    {
                        WorkingDirectory = "%Temp%"
                    },
                    new File("..\\Release\\cpuid.dll"),
                    new File("..\\Release\\CudaDeviceDetection.exe"),
                    new File("..\\Release\\Hardcodet.Wpf.TaskbarNotification.dll"),
                    new File("..\\Release\\LiveCharts.dll"),
                    new File("..\\Release\\LiveCharts.Wpf.dll"),
                    new File("..\\Release\\log4net.dll"),
                    new File("..\\Release\\MahApps.Metro.dll"),
                    new File("..\\Release\\MahApps.Metro.IconPacks.dll"),
                    new File("..\\Release\\MaterialDesignColors.dll"),
                    new File("..\\Release\\MaterialDesignThemes.MahApps.dll"),
                    new File("..\\Release\\MaterialDesignThemes.Wpf.dll"),
                    new File("..\\Release\\msvcp140.dll"),
                    new File("..\\Release\\Newtonsoft.Json.dll"),
                    new File("..\\Release\\nvidiasetp0state.exe"),
                    new File("..\\Release\\nvml.dll"),
                    new File("..\\Release\\OpenCL.dll"),
                    new File("..\\Release\\setcpuaff.exe"),
                    new File("..\\Release\\System.Windows.Interactivity.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.Data.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.GridView.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.Input.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.Navigation.dll"),
                    new File("..\\Release\\Telerik.Windows.Controls.RibbonView.dll"),
                    new File("..\\Release\\Telerik.Windows.Data.dll"),
                    new Dir("de",
                        new File("..\\Release\\de\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("es",
                        new File("..\\Release\\es\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("fr",
                        new File("..\\Release\\fr\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("it",
                        new File("..\\Release\\it\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("nl",
                        new File("..\\Release\\nl\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("tr",
                        new File("..\\Release\\tr\\Telerik.Windows.Controls.resources.dll")),
                    new Dir("langs",
                        new File("..\\Release\\langs\\en.lang"),
                        new File("..\\Release\\langs\\ru.lang"))),
                new Dir(@"%ProgramMenu%\bCash",
                    new ExeFileShortcut("bCash", "[INSTALLDIR]bCash.exe", "")
                    {
                        IconFile = @"..\..\AppFiles\icon_.ico"
                    },
                    new ExeFileShortcut("Uninstall bCash", "[System64Folder]msiexec.exe", "/x [ProductCode]")),
                new Dir("%Startup%",
                    new ExeFileShortcut("bCash", "[INSTALLDIR]bCash.exe", "")
                    {
                        Condition = new Condition("INSTALLSTARTUPSHORTCUT=\"yes\"") //property based condition
                    }),
                new ManagedAction(CustomActions.MyAction, "%this%", Return.ignore, When.Before, Step.LaunchConditions,
                    Condition.NOT_Installed, Sequence.InstallUISequence),
                new Property("INSTALLSTARTUPSHORTCUT", "no"),
                new Property("IDENTNUMBER", "-"),
                new Property("PAY_SYSTEM", "QIWI"),
                new RegValue(RegistryHive.CurrentUser,  @"Software\bCash", "Identification Number", "[IDENTNUMBER]"),
                new RegValue(RegistryHive.CurrentUser, @"Software\bCash", "Pay System", "[PAY_SYSTEM]")
            );
            project.UI = WUI.WixUI_Common;

            CustomUI customUI = new CustomUI();

            customUI.CustomDialogs.Add(productActivationDialog);
            customUI.On(NativeDialogs.InstallDirDlg, Buttons.ChangeFolder,
                new SetProperty("_BrowseProperty", "[WIXUI_INSTALLDIR]"),
                new ShowDialog(CommonDialogs.BrowseDlg));

            customUI.On(NativeDialogs.WelcomeDlg, Buttons.Next, new ShowDialog(productActivationDialog));

            customUI.On(productActivationDialog, Buttons.Next, new ShowDialog(NativeDialogs.InstallDirDlg));
            customUI.On(productActivationDialog, Buttons.Back, new ShowDialog(NativeDialogs.WelcomeDlg));
            customUI.On(productActivationDialog, Buttons.Cancel, new CloseDialog("Exit"));
            customUI.On(NativeDialogs.ExitDialog, Buttons.Finish, new CloseDialog() { Order = 9999 });

            customUI.On(NativeDialogs.InstallDirDlg, Buttons.Back, new ShowDialog(productActivationDialog));
            customUI.On(NativeDialogs.InstallDirDlg, Buttons.Next, new SetTargetPath(),
                new ShowDialog(NativeDialogs.VerifyReadyDlg));
            customUI.On(NativeDialogs.VerifyReadyDlg, Buttons.Back, new ShowDialog(NativeDialogs.InstallDirDlg, Condition.NOT_Installed),
                new ShowDialog(NativeDialogs.MaintenanceTypeDlg, Condition.Installed));
            customUI.On(NativeDialogs.MaintenanceWelcomeDlg, Buttons.Next, new ShowDialog(NativeDialogs.MaintenanceTypeDlg));

            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Back, new ShowDialog(NativeDialogs.MaintenanceWelcomeDlg));
            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Repair, new ShowDialog(NativeDialogs.VerifyReadyDlg));
            customUI.On(NativeDialogs.MaintenanceTypeDlg, Buttons.Remove, new ShowDialog(NativeDialogs.VerifyReadyDlg));

            project.CustomUI = customUI;
            project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");
            project.BuildMsi();
        }
    }


    public class CustomActions
    {
        [CustomAction]
        public static ActionResult MyAction(Session session)
        {
            if (DialogResult.Yes == MessageBox.Show("Do you want to start app with Windows?", "Installation", MessageBoxButtons.YesNo))
                session["INSTALLSTARTUPSHORTCUT"] = "yes";

            return ActionResult.Success;
        }
    }
}