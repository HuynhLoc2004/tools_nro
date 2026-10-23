using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace QLTK_DragonBoy
{
    // =========================================================================
    // QLTK_DragonBoy - Quản Lý Tài Khoản DragonBoy 2.5.0 PC (Bản Sạch 100%)
    // Mã nguồn mở an toàn, lưu trữ cục bộ, không kết nối mạng ra ngoài.
    // Tự động phân giải kích thước, xếp ô cửa sổ và tự động đăng nhập game.
    // =========================================================================

    public class Account
    {
        public bool Selected { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int ServerId { get; set; }
        public string Note { get; set; }

        public Account()
        {
            Selected = true;
            ServerId = 1;
            Username = "";
            Password = "";
            Note = "";
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        private List<Account> accounts = new List<Account>();
        private string dataFilePath;
        private string subDataFilePath;

        // UI Controls
        private DataGridView dgvAccounts;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private ComboBox cboServer;
        private TextBox txtNote;
        private NumericUpDown numWidth;
        private NumericUpDown numHeight;
        private NumericUpDown numCols;
        private NumericUpDown numDelay;
        private CheckBox chkShowPassword;
        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
        private Button btnRunSelected;
        private Button btnRunAll;
        private Button btnKillAll;
        private Label lblStatus;

        public MainForm()
        {
            dataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "accounts_safe.txt");
            subDataFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DragonBoy250_pc", "Data", "accounts_safe.txt");
            InitializeUI();
            LoadAccounts();
        }

        private void InitializeUI()
        {
            this.Text = "QLTK DragonBoy 2.5.0 - Quản Lý Tài Khoản [Mã Nguồn Mở]";
            this.Size = new Size(960, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Header Banner
            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(24, 119, 242)
            };
            Label lblTitle = new Label
            {
                Text = "QUẢN LÝ TÀI KHOẢN DRAGONBOY 2.5.0 (SẠCH & AN TOÀN 100%)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Location = new Point(15, 10),
                AutoSize = true
            };
            Label lblSub = new Label
            {
                Text = "Mã nguồn mở an toàn • Không gửi mật khẩu ra ngoài • Tự động xếp cửa sổ & auto login game",
                ForeColor = Color.FromArgb(220, 235, 252),
                Font = new Font("Segoe UI", 9f),
                Location = new Point(17, 36),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSub);
            this.Controls.Add(pnlHeader);

            // Group Nhập Liệu
            GroupBox grpInput = new GroupBox
            {
                Text = " Thông tin tài khoản ",
                Location = new Point(15, 75),
                Size = new Size(915, 95),
                BackColor = Color.White
            };

            Label lblUser = new Label { Text = "Tài khoản:", Location = new Point(15, 25), AutoSize = true };
            txtUsername = new TextBox { Location = new Point(15, 48), Width = 155 };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(185, 25), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(185, 48), Width = 150, UseSystemPasswordChar = true };

            chkShowPassword = new CheckBox { Text = "Hiện MK", Location = new Point(340, 50), AutoSize = true };
            chkShowPassword.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;

            Label lblSrv = new Label { Text = "Server:", Location = new Point(420, 25), AutoSize = true };
            cboServer = new ComboBox { Location = new Point(420, 48), Width = 105, DropDownStyle = ComboBoxStyle.DropDownList };
            for (int i = 1; i <= 15; i++) cboServer.Items.Add("Server " + i);
            cboServer.SelectedIndex = 0;

            Label lblNote = new Label { Text = "Ghi chú (Tên nv, nhiệm vụ...):", Location = new Point(540, 25), AutoSize = true };
            txtNote = new TextBox { Location = new Point(540, 48), Width = 160 };

            btnAdd = new Button
            {
                Text = "➕ Thêm",
                Location = new Point(715, 43),
                Size = new Size(88, 32),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnUpdate = new Button
            {
                Text = "💾 Sửa",
                Location = new Point(810, 43),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.Click += BtnUpdate_Click;

            grpInput.Controls.AddRange(new Control[] {
                lblUser, txtUsername, lblPass, txtPassword, chkShowPassword,
                lblSrv, cboServer, lblNote, txtNote, btnAdd, btnUpdate
            });
            this.Controls.Add(grpInput);

            // Bảng DataGridView
            dgvAccounts = new DataGridView
            {
                Location = new Point(15, 180),
                Size = new Size(915, 315),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            DataGridViewCheckBoxColumn colCheck = new DataGridViewCheckBoxColumn { HeaderText = "Chọn", Width = 50, DataPropertyName = "Selected" };
            DataGridViewTextBoxColumn colSTT = new DataGridViewTextBoxColumn { HeaderText = "STT", Width = 45, ReadOnly = true };
            DataGridViewTextBoxColumn colUser = new DataGridViewTextBoxColumn { HeaderText = "Tài khoản", Width = 160, ReadOnly = true, DataPropertyName = "Username" };
            DataGridViewTextBoxColumn colPass = new DataGridViewTextBoxColumn { HeaderText = "Mật khẩu", Width = 120, ReadOnly = true };
            DataGridViewTextBoxColumn colServer = new DataGridViewTextBoxColumn { HeaderText = "Máy chủ", Width = 85, ReadOnly = true };
            DataGridViewTextBoxColumn colNote = new DataGridViewTextBoxColumn { HeaderText = "Ghi chú", Width = 180, ReadOnly = true, DataPropertyName = "Note" };

            dgvAccounts.Columns.AddRange(new DataGridViewColumn[] { colCheck, colSTT, colUser, colPass, colServer, colNote });
            dgvAccounts.CellClick += DgvAccounts_CellClick;
            dgvAccounts.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvAccounts.IsCurrentCellDirty)
                {
                    dgvAccounts.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            dgvAccounts.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0 && e.RowIndex < accounts.Count)
                {
                    bool val = Convert.ToBoolean(dgvAccounts.Rows[e.RowIndex].Cells[0].Value);
                    accounts[e.RowIndex].Selected = val;
                }
            };
            this.Controls.Add(dgvAccounts);

            // Group Cấu Hình & Khởi Chạy
            GroupBox grpConfig = new GroupBox
            {
                Text = " Cấu hình hiển thị & Khởi chạy ",
                Location = new Point(15, 505),
                Size = new Size(915, 95),
                BackColor = Color.White
            };

            Label lblW = new Label { Text = "Rộng (W):", Location = new Point(15, 25), AutoSize = true };
            numWidth = new NumericUpDown { Location = new Point(15, 48), Width = 70, Minimum = 400, Maximum = 3840, Value = 1024 };

            Label lblH = new Label { Text = "Cao (H):", Location = new Point(95, 25), AutoSize = true };
            numHeight = new NumericUpDown { Location = new Point(95, 48), Width = 70, Minimum = 300, Maximum = 2160, Value = 600 };

            Label lblCols = new Label { Text = "Số cột xếp:", Location = new Point(175, 25), AutoSize = true };
            numCols = new NumericUpDown { Location = new Point(175, 48), Width = 70, Minimum = 1, Maximum = 8, Value = 2 };

            Label lblDelay = new Label { Text = "Độ trễ (ms):", Location = new Point(255, 25), AutoSize = true };
            numDelay = new NumericUpDown { Location = new Point(255, 48), Width = 75, Minimum = 500, Maximum = 10000, Value = 1500, Increment = 250 };

            btnDelete = new Button
            {
                Text = "🗑 Xoá Nick",
                Location = new Point(345, 43),
                Size = new Size(95, 33),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            btnRunSelected = new Button
            {
                Text = "▶ Chạy Nick Chọn",
                Location = new Point(455, 40),
                Size = new Size(140, 38),
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnRunSelected.FlatAppearance.BorderSize = 0;
            btnRunSelected.Click += (s, e) => RunGames(false);

            btnRunAll = new Button
            {
                Text = "🚀 Chạy Tất Cả",
                Location = new Point(605, 40),
                Size = new Size(140, 38),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnRunAll.FlatAppearance.BorderSize = 0;
            btnRunAll.Click += (s, e) => RunGames(true);

            btnKillAll = new Button
            {
                Text = "⛔ Đóng Hết Game",
                Location = new Point(755, 40),
                Size = new Size(145, 38),
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnKillAll.FlatAppearance.BorderSize = 0;
            btnKillAll.Click += BtnKillAll_Click;

            grpConfig.Controls.AddRange(new Control[] {
                lblW, numWidth, lblH, numHeight, lblCols, numCols, lblDelay, numDelay,
                btnDelete, btnRunSelected, btnRunAll, btnKillAll
            });
            this.Controls.Add(grpConfig);

            // Status strip
            lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 24,
                BackColor = Color.FromArgb(235, 238, 242),
                Text = " Sẵn sàng.",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8.5f)
            };
            this.Controls.Add(lblStatus);
        }

        // =====================================================================
        // QUẢN LÝ DỮ LIỆU CỤC BỘ (LƯU / ĐỌC)
        // =====================================================================

        private void LoadAccounts()
        {
            accounts.Clear();
            string loadPath = File.Exists(dataFilePath) ? dataFilePath : (File.Exists(subDataFilePath) ? subDataFilePath : null);
            if (loadPath != null)
            {
                try
                {
                    string[] lines = File.ReadAllLines(loadPath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || !line.Contains("|")) continue;
                        string[] parts = line.Split('|');
                        if (parts.Length >= 3)
                        {
                            int sid = 1;
                            int.TryParse(parts[1], out sid);
                            Account acc = new Account
                            {
                                Username = parts[0],
                                ServerId = sid > 0 ? sid : 1,
                                Password = DecodeSimple(parts[2]),
                                Note = parts.Length > 3 ? parts[3] : "",
                                Selected = true
                            };
                            accounts.Add(acc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi đọc file tài khoản: " + ex.Message);
                }
            }
            RefreshGrid();
        }

        private void SaveAccounts()
        {
            try
            {
                string dir = Path.GetDirectoryName(dataFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                List<string> lines = new List<string>();
                foreach (Account acc in accounts)
                {
                    lines.Add(acc.Username + "|" + acc.ServerId + "|" + EncodeSimple(acc.Password) + "|" + acc.Note);
                }
                File.WriteAllLines(dataFilePath, lines.ToArray());

                // Đồng bộ sang thư mục con nếu thư mục DragonBoy250_pc tồn tại
                string subDir = Path.GetDirectoryName(subDataFilePath);
                if (Directory.Exists(Path.GetDirectoryName(subDir)))
                {
                    if (!Directory.Exists(subDir)) Directory.CreateDirectory(subDir);
                    File.WriteAllLines(subDataFilePath, lines.ToArray());
                }

                lblStatus.Text = " Đã lưu " + accounts.Count + " tài khoản an toàn lúc " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu file: " + ex.Message);
            }
        }

        private string EncodeSimple(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return "";
            byte[] b = Encoding.UTF8.GetBytes(plain);
            return Convert.ToBase64String(b);
        }

        private string DecodeSimple(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return "";
            try
            {
                byte[] b = Convert.FromBase64String(encoded);
                return Encoding.UTF8.GetString(b);
            }
            catch { return encoded; }
        }

        private void RefreshGrid()
        {
            dgvAccounts.Rows.Clear();
            for (int i = 0; i < accounts.Count; i++)
            {
                Account acc = accounts[i];
                string maskPass = new string('•', Math.Min(acc.Password.Length, 10));
                dgvAccounts.Rows.Add(acc.Selected, (i + 1).ToString(), acc.Username, maskPass, "Server " + acc.ServerId, acc.Note);
            }
            lblStatus.Text = " Tổng số: " + accounts.Count + " tài khoản sẵn sàng.";
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tài khoản và mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int sid = cboServer.SelectedIndex + 1;
            accounts.Add(new Account
            {
                Username = user,
                Password = pass,
                ServerId = sid,
                Note = txtNote.Text.Trim(),
                Selected = true
            });

            SaveAccounts();
            RefreshGrid();

            txtUsername.Clear();
            txtPassword.Clear();
            txtNote.Clear();
            txtUsername.Focus();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.CurrentRow == null) return;
            int idx = dgvAccounts.CurrentRow.Index;
            if (idx >= 0 && idx < accounts.Count)
            {
                string user = txtUsername.Text.Trim();
                string pass = txtPassword.Text;
                if (!string.IsNullOrEmpty(user)) accounts[idx].Username = user;
                if (!string.IsNullOrEmpty(pass)) accounts[idx].Password = pass;
                accounts[idx].ServerId = cboServer.SelectedIndex + 1;
                accounts[idx].Note = txtNote.Text.Trim();

                SaveAccounts();
                RefreshGrid();
                MessageBox.Show("Đã cập nhật thông tin tài khoản!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAccounts.SelectedRows.Count == 0) return;
            if (MessageBox.Show("Bạn có chắc chắn muốn xoá các nick đã chọn khỏi bảng?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                List<Account> toRemove = new List<Account>();
                foreach (DataGridViewRow row in dgvAccounts.SelectedRows)
                {
                    int idx = row.Index;
                    if (idx >= 0 && idx < accounts.Count)
                    {
                        toRemove.Add(accounts[idx]);
                    }
                }
                foreach (Account acc in toRemove) accounts.Remove(acc);
                SaveAccounts();
                RefreshGrid();
            }
        }

        private void DgvAccounts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < accounts.Count)
            {
                Account acc = accounts[e.RowIndex];
                txtUsername.Text = acc.Username;
                txtPassword.Text = acc.Password;
                if (acc.ServerId >= 1 && acc.ServerId <= cboServer.Items.Count)
                {
                    cboServer.SelectedIndex = acc.ServerId - 1;
                }
                txtNote.Text = acc.Note;
            }
        }

        private void BtnKillAll_Click(object sender, EventArgs e)
        {
            try
            {
                int count = 0;
                string[] targetNames = new string[] { "DragonBoy250", "DragonBoy250_pc" };
                foreach (string name in targetNames)
                {
                    Process[] procs = Process.GetProcessesByName(name);
                    count += procs.Length;
                    foreach (var p in procs)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                lblStatus.Text = " Đã đóng " + count + " tiến trình game DragonBoy250.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đóng game: " + ex.Message);
            }
        }

        private string FindGameExecutable()
        {
            // 1. Kiểm tra ngay trong thư mục hiện tại cùng QLTK_DragonBoy.exe
            string pathSelf = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DragonBoy250.exe");
            if (File.Exists(pathSelf)) return pathSelf;

            // 2. Kiểm tra thư mục con DragonBoy250_pc
            string pathSub = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DragonBoy250_pc", "DragonBoy250.exe");
            if (File.Exists(pathSub)) return pathSub;

            // 3. Kiểm tra thư mục cha/anh em DragonBoy250_pc
            string pathParent = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "DragonBoy250_pc", "DragonBoy250.exe");
            if (File.Exists(pathParent)) return Path.GetFullPath(pathParent);

            return null;
        }

        private void RunGames(bool runAll)
        {
            string gameExe = FindGameExecutable();
            if (string.IsNullOrEmpty(gameExe) || !File.Exists(gameExe))
            {
                MessageBox.Show("Không tìm thấy file DragonBoy250.exe!\nHãy chắc chắn công cụ nằm cùng thư mục chứa game DragonBoy250.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Commit mọi thay đổi đang dở dang trên bảng
            dgvAccounts.EndEdit();

            List<Account> toRun = new List<Account>();
            if (runAll)
            {
                toRun.AddRange(accounts);
            }
            else
            {
                // 1. Quét trực tiếp ô checkbox trên giao diện bảng
                List<Account> checkedAccs = new List<Account>();
                for (int i = 0; i < accounts.Count && i < dgvAccounts.Rows.Count; i++)
                {
                    bool isChecked = false;
                    object val = dgvAccounts.Rows[i].Cells[0].Value;
                    if (val is bool)
                    {
                        isChecked = (bool)val;
                    }
                    else if (val != null)
                    {
                        bool.TryParse(val.ToString(), out isChecked);
                    }

                    if (isChecked)
                    {
                        checkedAccs.Add(accounts[i]);
                    }
                }

                if (checkedAccs.Count > 0)
                {
                    toRun.AddRange(checkedAccs);
                }
                else
                {
                    // 2. Nếu không có ô tích nào, ưu tiên lấy các dòng đang bôi đen / chọn
                    if (dgvAccounts.SelectedRows != null && dgvAccounts.SelectedRows.Count > 0)
                    {
                        List<int> selIndices = new List<int>();
                        foreach (DataGridViewRow r in dgvAccounts.SelectedRows)
                        {
                            selIndices.Add(r.Index);
                        }
                        selIndices.Sort();
                        foreach (int idx in selIndices)
                        {
                            if (idx >= 0 && idx < accounts.Count)
                            {
                                toRun.Add(accounts[idx]);
                            }
                        }
                    }
                    else if (dgvAccounts.CurrentRow != null)
                    {
                        int curIdx = dgvAccounts.CurrentRow.Index;
                        if (curIdx >= 0 && curIdx < accounts.Count)
                        {
                            toRun.Add(accounts[curIdx]);
                        }
                    }
                }
            }

            if (toRun.Count == 0)
            {
                MessageBox.Show("Chưa chọn tài khoản nào để chạy!\nHãy tích chọn ô hoặc nhấp chọn dòng tài khoản trong bảng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int w = (int)numWidth.Value;
            int h = (int)numHeight.Value;
            int cols = (int)numCols.Value;
            int delay = (int)numDelay.Value;
            string gameWorkDir = Path.GetDirectoryName(gameExe);

            Thread worker = new Thread(() =>
            {
                Rectangle screen = Screen.PrimaryScreen.WorkingArea;

                for (int i = 0; i < toRun.Count; i++)
                {
                    Account acc = toRun[i];
                    this.Invoke((MethodInvoker)(() =>
                    {
                        lblStatus.Text = string.Format(" [{0}/{1}] Đang khởi chạy: {2} (Server {3})...", i + 1, toRun.Count, acc.Username, acc.ServerId);
                    }));

                    // 1. Tạo Payload: Username|ServerId|Password
                    string payload = string.Format("{0}|{1}|{2}", acc.Username, acc.ServerId, acc.Password);

                    // 2. Mã hoá HEX UTF-8 chuẩn
                    byte[] bytes = Encoding.UTF8.GetBytes(payload);
                    StringBuilder hex = new StringBuilder();
                    foreach (byte b in bytes) hex.Append(b.ToString("X2"));

                    // 3. Khởi chạy DragonBoy250.exe <w> <h> <hex>
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = gameExe,
                        Arguments = string.Format("{0} {1} {2}", w, h, hex.ToString()),
                        WorkingDirectory = gameWorkDir
                    };

                    Process proc = null;
                    try
                    {
                        proc = Process.Start(psi);
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            lblStatus.Text = "Lỗi khởi chạy: " + ex.Message;
                        }));
                        continue;
                    }

                    // 4. Xếp vị trí lưới
                    int col = i % cols;
                    int row = i / cols;
                    int posX = screen.X + (col * w);
                    int posY = screen.Y + (row * h);

                    if (posX + w > screen.Right) posX = screen.X + ((i % 5) * 30);
                    if (posY + h > screen.Bottom) posY = screen.Y + ((i % 5) * 30);

                    Thread.Sleep(delay);

                    IntPtr hWnd = IntPtr.Zero;
                    if (proc != null)
                    {
                        for (int t = 0; t < 15; t++)
                        {
                            proc.Refresh();
                            if (proc.HasExited) break;
                            if (proc.MainWindowHandle != IntPtr.Zero)
                            {
                                hWnd = proc.MainWindowHandle;
                                break;
                            }
                            hWnd = FindWindow(null, "DragonBoy250");
                            if (hWnd != IntPtr.Zero) break;
                            Thread.Sleep(200);
                        }
                    }

                    if (hWnd != IntPtr.Zero)
                    {
                        MoveWindow(hWnd, posX, posY, w, h, true);
                        SetWindowText(hWnd, string.Format("DragonBoy250 [{0}] {1} (S{2})", i + 1, acc.Username, acc.ServerId));
                    }
                }

                this.Invoke((MethodInvoker)(() =>
                {
                    lblStatus.Text = string.Format(" Đã mở thành công {0} tài khoản!", toRun.Count);
                }));
            });

            worker.IsBackground = true;
            worker.Start();
        }

        [STAThread]
        public static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                try { File.WriteAllText("qltk_error.log", ex.ToString()); } catch { }
                MessageBox.Show(ex.ToString(), "Lỗi khởi động QLTK");
            }
        }
    }
}
