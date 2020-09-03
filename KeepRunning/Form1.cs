using AutoUpdaterDotNET;
using ClassLibraryHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeepRunning
{
    public partial class Form1 : Form
    {

        #region CONFIG

        const string FILE_CONFIG = "FILE_CONFIG.xml";
        CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private BindingList<AppInfo> _bindingAppInfo;
        public const string FILE_RUN = "_run.keeprunning";
        public const string FILE_UPDATE = "_update.keeprunning";
        const string FILE_RESTART = "_restart.keeprunning";
        public TimeSpan? TimeRestartApp { get; set; }

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var _timeRestartApp = ConfigHelper.GetConfig("TimeRestartApp", "04:00:00");
            if (TimeSpan.TryParse(_timeRestartApp, out TimeSpan x))
            {
                this.TimeRestartApp = x;
            }
            else
            {
                this.TimeRestartApp = null;
            }
            label_restart.Text = $"RestartApps: {TimeRestartApp?.ToString() ?? "No restart"}";
            _initForm();
            _reRun();
        }

        private void _reRun()
        {
            var _lstAppInfo = new List<AppInfo>();
            MethodHelper.UseTryCatch(() =>
            {
                if (File.Exists(FILE_CONFIG))
                {
                    _lstAppInfo = XmlHelper.DeserializeFromXmlFile<List<AppInfo>>(FILE_CONFIG);
                }

                _bindingAppInfo = new BindingList<AppInfo>(_lstAppInfo);
                dataGridView1.DataSource = _bindingAppInfo;

                cancellationToken?.Cancel();
                cancellationToken = new CancellationTokenSource();
                cancellationToken.Token.ThrowIfCancellationRequested();

                Task.Run(async () =>
                {
                    await MethodHelper.UseTryCatch(async () =>
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            _setDateTime();
                            var _10 = DateTime.Now.Second % 10;
                            if (_10 == 0)
                            {
                                foreach (var item in _bindingAppInfo)
                                {
                                    try
                                    {
                                        //check run
                                        var file_run = Path.Combine(item.Folder, FILE_RUN);
                                        if (File.Exists(file_run))
                                        {
                                            File.Delete(file_run);
                                            item.CountNotRun = 0;
                                            item.UpdateExpiredTime = null;
                                            item.Message = "OK";
                                        }
                                        else
                                        {
                                            //isupdate
                                            var file_update = Path.Combine(item.Folder, FILE_RUN);
                                            if (File.Exists(file_update))
                                            {
                                                File.Delete(file_update);
                                                item.UpdateExpiredTime = DateTime.Now.AddMinutes(30);
                                                item.Message = $"Updating... Expired: {item.UpdateExpiredTime:HH:mm:ss dd/MM/yyyy}";
                                            }
                                            else if (item.UpdateExpiredTime > DateTime.Now != true)
                                            {
                                                if (item.TypeCheckRun == eTypeCheckRun.ByProcessName)
                                                {
                                                    var f = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.PathExe)).FirstOrDefault(q => q.MainModule.FileName == item.PathExe);
                                                    if (f != null)
                                                    {
                                                        if (f.Responding)
                                                        {
                                                            item.CountNotRun = 0;
                                                            item.Message = "OK";
                                                            continue;
                                                        }
                                                        else
                                                        {
                                                            item.Message = "Not Responding";
                                                        }
                                                    }
                                                }
                                                item.CountNotRun++;
                                                if (item.CountNotRun >= 15)
                                                {
                                                    if (ConfigHelper.GetConfig("RestartPC", false))
                                                    {
                                                        item.Message = "Restart PC...";
                                                        PCHelper.RestartPC();
                                                        await Task.Delay(5000);
                                                    }
                                                    else
                                                    {
                                                        item.Message = "Not Restart PC, reset count";
                                                    }
                                                    item.CountNotRun = 0;
                                                }
                                                else if (item.CountNotRun >= 3)
                                                {
                                                    //kill process and re-open
                                                    if (File.Exists(item.PathExe))
                                                    {
                                                        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.PathExe)))
                                                        {
                                                            p.Kill();
                                                        }
                                                        Process.Start(item.PathExe);
                                                        item.Message = "re-opened";
                                                    }
                                                    else
                                                    {
                                                        item.Message = "not found .exe";
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        item.Message = ex.Message;
                                    }
                                }

                                dataGridView1.InvokeHelper(q => q.AutoResizeColumns());
                            }
                            else
                            {
                                if (TimeRestartApp != null)
                                {
                                    var time_now = DateTime.Now.OnlyTime();
                                    if (TimeRestartApp <= time_now && time_now <= TimeRestartApp?.Add(TimeSpan.FromMinutes(5)))
                                    {
                                        foreach (var item in _bindingAppInfo)
                                        {
                                            try
                                            {
                                                FileInfo infoFileRestart = new FileInfo(Path.Combine(item.Folder, FILE_RESTART));
                                                if (infoFileRestart.Exists != true || infoFileRestart.LastWriteTime.OnlyDate() != DateTime.Now.OnlyDate())
                                                {
                                                    if (infoFileRestart.Exists != true)
                                                    {
                                                        infoFileRestart.Create().Close();
                                                    }
                                                    infoFileRestart.LastWriteTime = DateTime.Now;

                                                    //kill process and re-open
                                                    if (File.Exists(item.PathExe))
                                                    {
                                                        foreach (var p in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(item.PathExe)))
                                                        {
                                                            if (p.MainModule.FileName == item.PathExe)
                                                            {
                                                                p.Kill();
                                                            }
                                                        }
                                                        Process.Start(item.PathExe);
                                                        item.Message = "re-opened";
                                                    }
                                                    else
                                                    {
                                                        item.Message = "not found .exe";
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                item.Message = ex.Message;
                                            }
                                        }
                                    }
                                }
                            }

                            await Task.Delay(900, cancellationToken.Token);
                        }
                    });
                }, cancellationToken.Token);
            });
        }

        private void _setDateTime()
        {
            label_datetime.InvokeHelper(q => q.Text = DateTime.Now.ToString("dddd HH:mm:ss dd/MM/yyyy"));
        }

        private void _initForm()
        {
            dataGridView1.AutoGenerateColumns = false;
            Column_KieuCheck.DataSource = Enum.GetValues(typeof(eTypeCheckRun));

            var pros = new[] {
                nameof(AppInfo.Name),
                nameof(AppInfo.Message),
                nameof(AppInfo.CountNotRun),
                nameof(AppInfo.TypeCheckRun),
                nameof(AppInfo.PathExe),
            };
            for (int i = 0; i < pros.Length; i++)
            {
                dataGridView1.Columns[i].DataPropertyName = pros[i];
            }

            notifyIcon1.Icon = Icon;
            notifyIcon1.Text = Text;
        }

        private void xóaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                if (dataGridView1.SelectedCells[0].OwningRow.DataBoundItem is AppInfo appInfo)
                {
                    if (MessageBox.Show($"Bạn muốn xóa {appInfo.Name}", "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        var f = _bindingAppInfo.FirstOrDefault(a => a.PathExe == appInfo.PathExe);
                        if (f != null)
                        {
                            dataGridView1.InvokeHelper(q =>
                            {
                                cancellationToken.Cancel();
                                _bindingAppInfo.Remove(f);

                                _save();
                                _reRun();
                            });
                        }
                    }
                }
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(q => q.ToLower().EndsWith(".exe")).ToList();
            if (files.Any())
            {
                var d_new = files.Where(a => _bindingAppInfo.Any(q => q.PathExe == a) == false);
                foreach (var item in d_new)
                {
                    _bindingAppInfo.Add(new AppInfo
                    {
                        CountNotRun = 0,
                        Folder = Path.GetDirectoryName(item),
                        Message = "---",
                        Name = Path.GetFileNameWithoutExtension(item),
                        PathExe = item,
                        UpdateExpiredTime = null,
                        TypeCheckRun = eTypeCheckRun.ByFileRun
                    });
                }

                _save();
                _reRun();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = WindowState == FormWindowState.Minimized ? FormWindowState.Normal : FormWindowState.Minimized;
            ShowInTaskbar = WindowState != FormWindowState.Minimized;
        }

        private void chạyPhầnMềmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedCells.Count > 0)
            {
                if (dataGridView1.SelectedCells[0].OwningRow.DataBoundItem is AppInfo appInfo)
                {
                    if (MessageBox.Show($"Bạn muốn chạy {appInfo.Name}", "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    {
                        if (File.Exists(appInfo.PathExe))
                        {
                            Process.Start(appInfo.PathExe);
                        }
                        else
                        {
                            MessageBox.Show($"{appInfo.PathExe} không tồn tại!");
                        }
                    }
                }
            }
        }

        private void thoátToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cancellationToken?.Cancel();
            cancellationToken = null;
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancellationToken = null;
            cancellationToken?.Cancel();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _save();
        }

        private void _save()
        {
            dataGridView1.EndEdit();
            XmlHelper.SerializeToXmlFile(_bindingAppInfo.ToList(), FILE_CONFIG);
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        { }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _reRun();
        }

        private void checkForUpdareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Kiểm tra cập nhật?", "Xác nhận", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                AutoUpdater.Start("https://github.com/TiepHoangDev/KeepRunning/raw/master/KeepRunning/autoupdate/AutoUpdater.xml");

                AutoUpdater.CheckForUpdateEvent += (arg) =>
                {
                    if (arg.IsUpdateAvailable)
                    {
                        AutoUpdater.ShowUpdateForm(arg);
                    }
                    else
                    {
                        MessageBox.Show("Không có bản cập nhật mới");
                    }
                };
            }
        }
    }

    public class KeepRunningClient
    {
        public KeepRunningClient(Func<bool> AppIsRunning)
        {
            this.AppIsRunning = AppIsRunning;
        }

        public Func<bool> AppIsRunning { get; }
        public bool IsUpdate { get; private set; }

        public void Start()
        {
            _ = Task.Run(async () =>
              {
                  FileAndFolderExtention.DeleteFileIfExist(Form1.FILE_UPDATE);
                  if (!File.Exists(Form1.FILE_RUN))
                  {
                      File.Create(Form1.FILE_RUN).Close();
                  }

                  while (AppIsRunning?.Invoke() != false)
                  {
                      var d = DateTime.Now.Second % 10;
                      MethodHelper.UseTryCatch(() =>
                      {
                          if (d == 5)
                          {
                              if (!IsUpdate)
                              {
                                  if (!File.Exists(Form1.FILE_RUN))
                                  {
                                      File.Create(Form1.FILE_RUN).Close();
                                  }
                              }

                              FileAndFolderExtention.DeleteFileIfExist(Form1.FILE_UPDATE);
                          }
                      });

                      await Task.Delay(900);
                  }
              });
        }

        /// <summary>
        /// Set State to update and create file flag update
        /// </summary>
        /// <param name="isUpdate"></param>
        public void SetUpdate(bool isUpdate)
        {
            this.IsUpdate = isUpdate;
            File.Create(Form1.FILE_UPDATE).Close();
        }
    }

    public enum eTypeCheckRun
    {
        ByProcessName,
        ByFileRun
    }
}
