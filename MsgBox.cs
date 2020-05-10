using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
// ReSharper disable InheritdocConsiderUsage
// ReSharper disable VirtualMemberCallInConstructor

namespace AhDung.WinForm
{
    /// <summary>
    /// 可以携带详细信息的消息框
    /// <para>- buttonsText参数可自定义按钮文本，元素顺序对应按钮从左到右的顺序，缺少对应元素或对应元素为NullOrEmpty的按钮将沿用默认文本，多出的元素则忽略</para>
    /// </summary>
    public static class MsgBox
    {
        //异常消息文本
        const string InvalidButtonExString   = "按钮参数不是有效的枚举项！";
        const string InvalidIconExString     = "图标参数不是有效的枚举项！";
        const string InvalidDfButtonExString = "默认按钮参数不是有效的枚举项！";

        //提示情景标题
        const string InfoCaption    = "提示...";
        const string WarningCaption = "警告...";
        const string ErrorCaption   = "错误...";

        /// <summary>
        /// 是否启用动画效果
        /// </summary>
        public static bool EnableAnimate { get; set; } = true;

        /// <summary>
        /// 是否启用声音反馈
        /// </summary>
        public static bool EnableSound { get; set; } = true;

        #region 公开方法

        /// <summary>
        /// 显示信息框
        /// </summary>
        public static void ShowInfo(string message, string attach = null, string caption = InfoCaption, bool expand = false, string buttonText = null) =>
            ShowCore(message, caption, attach, icon: MessageBoxIcon.Information, expand: expand, buttonsText: new[] { buttonText });

        /// <summary>
        /// 显示警告框
        /// </summary>
        public static void ShowWarning(string message, string attach = null, string caption = WarningCaption, bool expand = false, string buttonText = null) =>
            ShowCore(message, caption, attach, icon: MessageBoxIcon.Warning, expand: expand, buttonsText: new[] { buttonText });

        /// <summary>
        /// 显示警告框
        /// </summary>
        public static void ShowWarning(string message, Exception exception, string caption = WarningCaption, bool expand = false, string buttonText = null) =>
            ShowCore(message, caption, exception?.ToString(), icon: MessageBoxIcon.Warning, expand: expand, buttonsText: new[] { buttonText });

        /// <summary>
        /// 显示错误框
        /// </summary>
        public static void ShowError(string message, string attach = null, string caption = ErrorCaption, bool expand = false, string buttonText = null) =>
            ShowCore(message, caption, attach, icon: MessageBoxIcon.Error, expand: expand, buttonsText: new[] { buttonText });

        /// <summary>
        /// 显示错误框
        /// </summary>
        public static void ShowError(string message, Exception exception, string caption = ErrorCaption, bool expand = false, string buttonText = null) =>
            ShowCore(message, caption, exception?.ToString(), icon: MessageBoxIcon.Error, expand: expand, buttonsText: new[] { buttonText });

        /// <summary>
        /// 显示询问框
        /// </summary>
        public static DialogResult ShowQuestion(string message, string attach = null, string caption = InfoCaption, MessageBoxButtons buttons = MessageBoxButtons.OKCancel, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, bool expand = false, string[] buttonsText = null) =>
            ShowCore(message, caption, attach, buttons, MessageBoxIcon.Question, defaultButton, expand, buttonsText);


        /// <summary>
        /// 显示消息框
        /// </summary>
        /// <param name="message">消息文本</param>
        /// <param name="caption">消息框标题</param>
        /// <param name="attach">附加消息</param>
        /// <param name="buttons">按钮组合</param>
        /// <param name="icon">图标</param>
        /// <param name="defaultButton">默认按钮</param>
        /// <param name="expand">展开详细信息（仅当存在附加消息时有效）</param>
        /// <param name="buttonsText">按钮文本</param>
        public static DialogResult Show(string message, string caption = null, string attach = null, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, bool expand = false, string[] buttonsText = null)
        {
            if (!Enum.IsDefined(typeof(MessageBoxButtons), buttons)) { throw new InvalidEnumArgumentException(InvalidButtonExString); }
            if (!Enum.IsDefined(typeof(MessageBoxIcon), icon)) { throw new InvalidEnumArgumentException(InvalidIconExString); }
            if (!Enum.IsDefined(typeof(MessageBoxDefaultButton), defaultButton)) { throw new InvalidEnumArgumentException(InvalidDfButtonExString); }

            return ShowCore(message, caption, attach, buttons, icon, defaultButton, expand, buttonsText);
        }

        /// <summary>
        /// 显示消息框
        /// </summary>
        /// <param name="message">消息文本</param>
        /// <param name="caption">消息框标题</param>
        /// <param name="exception">异常</param>
        /// <param name="buttons">按钮组合</param>
        /// <param name="icon">图标</param>
        /// <param name="defaultButton">默认按钮</param>
        /// <param name="expand">展开详细信息（仅当异常不为空时有效）</param>
        /// <param name="buttonsText">按钮文本</param>
        public static DialogResult Show(string message, string caption, Exception exception, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, bool expand = false, string[] buttonsText = null) =>
            Show(message, caption, exception?.ToString(), buttons, icon, defaultButton, expand, buttonsText);

        #endregion

        private static DialogResult ShowCore(string message, string caption = null, string attach = null, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1, bool expand = false, string[] buttonsText = null)
        {
            using (var f = new MessageForm(message, caption, buttons, icon, defaultButton, attach, EnableAnimate, EnableSound, expand, buttonsText))
            {
                return f.ShowDialog();
            }
        }


        /*----------------
         下面是消息窗体相关
         ---------------*/

        //参数有效性由MsgBox负责
        /// <summary>
        /// 消息窗体
        /// </summary>
        private class MessageForm : Form
        {
            /* todo 已知细小问题：
             * 当消息区文本非常非常多时，且反复进行改变消息框窗口大小、位置、展开收起的操作，那么在某次展开时
               详细信息文本框可能会在原位置（即消息区内某rect）瞬闪一下，
               原因是文本框控件在显示时总会在原位置WM_NCPAINT + WM_ERASEBKGND一下，暂无解决办法。
               实际应用中碰到的几率很小，就算碰到，影响也可以忽略。
             */

            const int MaxClientWidth = 700; //最大默认窗体客户区宽度
            static readonly Font GlobalFont = SystemFonts.MessageBoxFont;

            readonly string _messageSound;
            readonly bool _expand;
            bool _useAnimate;
            readonly bool _useSound;
            readonly MessageBoxButtons _buttons;
            readonly bool _hasAttach;
            readonly ToggleButton _ckbToggle;
            readonly MessageViewer _msgViewer;
            readonly PanelBasic _panelButtons;
            readonly PanelBasic _panelAttach;

            int _expandHeight;
            /// <summary>
            /// 详细信息区展开高度
            /// </summary>
            private int ExpandHeight
            {
                get => _expandHeight < 150 ? 150 : _expandHeight;
                set => _expandHeight = value;
            }

            /// <summary>
            /// 创建消息窗体
            /// </summary>
            public MessageForm(string message, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton, string attach, bool enableAnimate, bool enableSound, bool expand, string[] buttonsText)
            {
                _useAnimate = enableAnimate;
                _useSound = enableSound;
                _buttons = buttons;
                _expand = expand;
                _hasAttach = !string.IsNullOrEmpty(attach);
                _msgViewer = CreateMessageViewer(icon, message, out _messageSound);
                _panelButtons = CreateButtonsPanel(_hasAttach, _useAnimate, _buttons, defaultButton, buttonsText, out var createdButtons, out int dfBtnIdx);
                if (_hasAttach)
                {
                    _ckbToggle = (ToggleButton)createdButtons[0];
                    _ckbToggle.CheckedChanged += ckbToggle_CheckedChanged;
                }

                SuspendLayout();

                Controls.Add(_msgViewer);
                Controls.Add(_panelButtons);
                if (_hasAttach)
                {
                    _panelAttach = CreateAttachPanel(attach);
                    _panelAttach.Resize += plAttachZone_Resize;
                    Controls.Add(_panelAttach);
                }
                StartPosition = ActiveForm == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent;
                Font = GlobalFont;
                DoubleBuffered = true;
                MaximizeBox = false;
                Name = "MessageForm";
                Padding = new Padding(0, 0, 0, 17);
                ShowIcon = false;
                ShowInTaskbar = false;
                TopMost = true;
                SizeGripStyle = SizeGripStyle.Show;
                Text = caption;
                AcceptButton = (Button)createdButtons[dfBtnIdx];

                //只有【确定】或有【取消】按钮时允许按ESC关闭
                if (_buttons == MessageBoxButtons.OK || ((int)_buttons & 1) == 1)
                {
                    CancelButton = (Button)createdButtons[createdButtons.Length - 1];
                }

                MinimumSize = SizeFromClientSize(new Size(_panelButtons.MinimumSize.Width + Padding.Horizontal, _msgViewer.MinimumSize.Height + _panelButtons.MinimumSize.Height + Padding.Vertical));
                ClientSize = GetPreferredSize(new Size(MaxClientWidth, Screen.PrimaryScreen.WorkingArea.Height - (Height - ClientSize.Height)));

                ResumeLayout(false);
                PerformLayout();
            }

            #region 重写基类

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);

                if (_hasAttach && _expand)
                {
                    var animate = _useAnimate;
                    _useAnimate = false;
                    _ckbToggle.Checked = true;
                    _useAnimate = animate;
                }
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);

                //设置默认按钮焦点。须在OnShown中设置按钮焦点才有用
                (AcceptButton as Button)?.Focus();

                //播放消息提示音
                if (_useSound) { PlaySystemSound(_messageSound); }

                TopMost = false;
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;

                    if (((int)_buttons & 1) == 0) //没有Cancel按钮时屏蔽关闭按钮，刚好在偶数项
                    {
                        cp.ClassStyle |= 0x200;
                    }

                    return cp;
                }
            }

            /// <summary>
            /// 计算合适的窗口尺寸
            /// </summary>
            /// <param name="proposedSize">该参数此处定义为客户区可设置的最大尺寸</param>
            public override Size GetPreferredSize(Size proposedSize)
            {
                var reservedHeight = _panelButtons.Height + Padding.Bottom;
                var size = _msgViewer.GetPreferredSize(new Size(proposedSize.Width, proposedSize.Height - reservedHeight));
                size.Height += reservedHeight;
                return size;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_ckbToggle != null)
                    {
                        _ckbToggle.CheckedChanged -= ckbToggle_CheckedChanged;
                        _ckbToggle.Dispose();
                    }
                }
                base.Dispose(disposing);
            }

            #endregion

            #region 事件处理方法

            //展开收起
            private void ckbToggle_CheckedChanged(object sender, EventArgs e)
            {
                SuspendLayout();

                if (_ckbToggle.Checked)
                {
                    _panelButtons.SendToBack();
                    _msgViewer.SendToBack();

                    _msgViewer.Dock = DockStyle.Top;
                    _panelButtons.Dock = DockStyle.Top;

                    ChangeFormHeight(ExpandHeight);
                    _panelAttach.Visible = true;
                }
                else
                {
                    ExpandHeight = _panelAttach.Height;//为再次展开记忆高度
                    _panelAttach.Visible = false;
                    ChangeFormHeight(-_panelAttach.Height);//收起时直接取pl高度，不要取ExpandHeight

                    _panelButtons.SendToBack();

                    _panelButtons.Dock = DockStyle.Bottom;
                    _msgViewer.Dock = DockStyle.Fill;
                }

                ResumeLayout();
            }

            //用户手工收完详细区则触发折叠
            private void plAttachZone_Resize(object sender, EventArgs e)
            {
                if (_ckbToggle.Checked && _panelAttach.Height == 0
                    && WindowState != FormWindowState.Minimized) //最小化也会触发该事件，所以要排除
                {
                    _ckbToggle.Checked = false;
                }
            }

            #endregion

            #region 辅助+私有方法

            static MessageViewer CreateMessageViewer(MessageBoxIcon icon, string text, out string sound)
            {
                Icon ico;
                switch (icon)
                {
                    //MessageBoxIcon.Information同样
                    case MessageBoxIcon.Asterisk:
                        ico = SystemIcons.Information;
                        sound = "SystemAsterisk";
                        break;

                    //MessageBoxIcon.Hand、MessageBoxIcon.Stop同样
                    case MessageBoxIcon.Error:
                        ico = SystemIcons.Error;
                        sound = "SystemHand";
                        break;

                    //MessageBoxIcon.Warning同样
                    case MessageBoxIcon.Exclamation:
                        ico = SystemIcons.Warning;
                        sound = "SystemExclamation";
                        break;

                    case MessageBoxIcon.Question:
                        ico = SystemIcons.Question;
                        sound = "SystemAsterisk";//Question原本是没声音的，此实现让它蹭一下Information的
                        break;

                    default: //MessageBoxIcon.None
                        ico = null;
                        sound = "SystemDefault";
                        break;
                }

                var view = new MessageViewer();
                view.SuspendLayout();

                view.Font = GlobalFont;
                view.Dock = DockStyle.Fill;
                view.Icon = ico;
                view.Text = text;
                view.Padding = new Padding(21, 18, 21, 18);
                view.MinimumSize = new Size((ico?.Width ?? 0) + view.Padding.Horizontal, Math.Max(ico?.Height ?? 0, GlobalFont.Height) + view.Padding.Vertical);

                view.ResumeLayout(false);
                return view;
            }

            static PanelBasic CreateButtonsPanel(bool hasAttach, bool useAnimate, MessageBoxButtons buttons, MessageBoxDefaultButton defaultButton, string[] buttonsText, out Control[] createdButtons, out int defaultButtonIndex)
            {
                // 由于CreateButtonsPanel时仍未加入Form，所以字体尚未继承Form，
                // 此时依赖字体的尺寸计算都不可靠，所以创建按钮时需指定字体

                const int PADDING = 10; //按钮距边
                const int SPACING = 3; //按钮间距

                var buttonList = new LinkedList<Control>();
                var width = PADDING;
                if (hasAttach)
                {
                    var btn = new ToggleButton(useAnimate) { Font = GlobalFont, MinimumSize = new Size(93, 27), Text = "详细信息(&D)", Location = new Point(width, PADDING) };
                    btn.Size = btn.MinimumSize;
                    buttonList.AddLast(btn);
                    width += btn.Width + SPACING + 10; // 详细信息按钮 与 正常按钮之间多间隔一点
                }
                switch (buttons)
                {
                    case MessageBoxButtons.AbortRetryIgnore:
                        defaultButtonIndex = buttonList.Count + (int)defaultButton / 0x100;
                        width += Add(width, GetText(0) ?? "中止(&A)", DialogResult.Abort).Width + SPACING;
                        width += Add(width, GetText(1) ?? "重试(&R)", DialogResult.Retry).Width + SPACING;
                        width += Add(width, GetText(2) ?? "忽略(&I)", DialogResult.Ignore).Width + PADDING;
                        break;
                    case MessageBoxButtons.OK:
                        defaultButtonIndex = buttonList.Count;
                        width += Add(width, GetText(0) ?? "确定(&O)", DialogResult.OK).Width + PADDING;
                        break;
                    case MessageBoxButtons.OKCancel:
                        defaultButtonIndex = buttonList.Count + (defaultButton == MessageBoxDefaultButton.Button2 ? 1 : 0);
                        width += Add(width, GetText(0) ?? "确定(&O)", DialogResult.OK).Width + SPACING;
                        width += Add(width, GetText(1) ?? "取消(&C)", DialogResult.Cancel).Width + PADDING;
                        break;
                    case MessageBoxButtons.RetryCancel:
                        defaultButtonIndex = buttonList.Count + (defaultButton == MessageBoxDefaultButton.Button2 ? 1 : 0);
                        width += Add(width, GetText(0) ?? "重试(&R)", DialogResult.Retry).Width + SPACING;
                        width += Add(width, GetText(1) ?? "取消(&C)", DialogResult.Cancel).Width + PADDING;
                        break;
                    case MessageBoxButtons.YesNo:
                        defaultButtonIndex = buttonList.Count + (defaultButton == MessageBoxDefaultButton.Button2 ? 1 : 0);
                        width += Add(width, GetText(0) ?? "是(&Y)", DialogResult.Yes).Width + SPACING;
                        width += Add(width, GetText(1) ?? "否(&N)", DialogResult.No).Width + PADDING;
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        defaultButtonIndex = buttonList.Count + (int)defaultButton / 0x100;
                        width += Add(width, GetText(0) ?? "是(&Y)", DialogResult.Yes).Width + SPACING;
                        width += Add(width, GetText(1) ?? "否(&N)", DialogResult.No).Width + SPACING;
                        width += Add(width, GetText(2) ?? "取消(&C)", DialogResult.Cancel).Width + PADDING;
                        break;
                    default: throw new InvalidEnumArgumentException();
                }

                createdButtons = new Control[buttonList.Count];
                buttonList.CopyTo(createdButtons, 0);

                var pl = new PanelBasic();
                pl.SuspendLayout();

                pl.Size = pl.MinimumSize = new Size(width, createdButtons[0].Height + PADDING);
                pl.Dock = DockStyle.Bottom;
                pl.Controls.AddRange(createdButtons);

                pl.ResumeLayout(false);
                return pl;

                Button Add(int left, string text, DialogResult result)
                {
                    var btn = new Button { Font = GlobalFont, AutoSize = true, Text = text, MinimumSize = new Size(85, 27), Anchor = AnchorStyles.Right, DialogResult = result };
                    btn.Size = btn.PreferredSize;
                    btn.Location = new Point(left, PADDING);
                    buttonList.AddLast(btn);
                    return btn;
                }

                //除非确实有文字，其他情况一律返回null
                string GetText(int i)
                {
                    if (buttonsText != null && buttonsText.Length > i)
                    {
                        var text = buttonsText[i];
                        if (!string.IsNullOrEmpty(text))
                        {
                            return text;
                        }
                    }

                    return null;
                }
            }

            static PanelBasic CreateAttachPanel(string attach)
            {
                var txb = new TextBox
                {
                    Anchor = (AnchorStyles)15, //上下左右
                    Location = new Point(10, 7),
                    ReadOnly = true,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = attach
                };

                // Ctrl+A全选
                txb.KeyDown += (_, e) =>
                {
                    if (e.Control && e.KeyCode == Keys.A)
                    {
                        txb.SelectAll();
                    }
                };

                var panel = new PanelBasic();
                panel.SuspendLayout();

                panel.Dock = DockStyle.Fill;
                panel.Visible = false;
                panel.Size = new Size(txb.Width + 20, txb.Height + 9);
                panel.Controls.Add(txb);

                panel.ResumeLayout(false);
                return panel;
            }

            /// <summary>
            /// 改变窗体高度。内部有动画处理
            /// </summary>
            /// <param name="delta">增量（负数为减小高度）</param>
            private void ChangeFormHeight(int delta)
            {
                //先得到精确的目标高度
                var target = Height + delta;

                //若要跑动画，只跑总帧数-1帧
                if (_useAnimate)
                {
                    const int frames = 8;
                    var per = delta / frames; //每帧平均动

                    for (int i = 1; i < frames; i++)
                    {
                        Height += per;

                        Application.DoEvents();
                        Thread.Sleep(10);
                    }
                }

                //最后直达目标
                Height = target;
            }

            /// <summary>
            /// 播放系统事件声音
            /// </summary>
            /// <remarks>之所以不用MessageBeep API是因为这货在srv08上不出声，所以用PlaySound代替</remarks>
            private static void PlaySystemSound(string soundAlias)
            {
                PlaySound(soundAlias, IntPtr.Zero, 0x10000 /*SND_ALIAS*/| 0x1 /*SND_ASYNC*/);
            }

            #endregion

            #region 嵌套类

            /// <summary>
            /// 基础面板
            /// </summary>
            private class PanelBasic : Control
            {
                public PanelBasic()
                {
                    SetStyle(ControlStyles.AllPaintingInWmPaint, false);//关键，不然其上的ToolBar不正常
                    SetStyle(ControlStyles.OptimizedDoubleBuffer, true);//重要。不设置的话控件绘制不正常
                    SetStyle(ControlStyles.ContainerControl, true);
                    SetStyle(ControlStyles.Selectable, false);
                }

                protected override void WndProc(ref Message m)
                {
                    //屏蔽WM_ERASEBKGND。防止显示时在原位置快闪
                    //不能通过ControlStyles.AllPaintingInWmPaint=true屏蔽
                    //会影响其上的ToolBar
                    if (m.Msg == 0x14) { return; }

                    base.WndProc(ref m);
                }

                protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
                {
                    //防Dock时面板短暂滞留在原位置
                    base.SetBoundsCore(x, y, width, height, specified | BoundsSpecified.Y | BoundsSpecified.Width);
                }
            }

            /// <summary>
            /// 消息呈现控件
            /// </summary>
            private class MessageViewer : Control
            {
                const TextFormatFlags TEXTFLAGS = TextFormatFlags.EndEllipsis        //未完省略号
                                                  | TextFormatFlags.WordBreak        //允许换行
                                                  | TextFormatFlags.NoPadding        //无边距
                                                  | TextFormatFlags.ExternalLeading  //行间空白。NT5必须，不然文字挤在一起
                                                  | TextFormatFlags.TextBoxControl   //避免半行
                                                  | TextFormatFlags.NoPrefix         //不把 &X 视为助记键
                                                  ;

                const int IconSpace = 10; //图标与文本间距
                const float PreferredScale = 12;//最佳文本区块比例（宽/高）

                /// <summary>
                /// 获取或设置图标
                /// </summary>
                public Icon Icon { get; set; }

                public MessageViewer()
                {
                    SetStyle(ControlStyles.CacheText, true);
                    SetStyle(ControlStyles.UserPaint, true);
                    SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                    SetStyle(ControlStyles.Selectable, false);
                    SetStyle(ControlStyles.ResizeRedraw, true); //重要

                    DoubleBuffered = true; //双缓冲
                    BackColor = Environment.OSVersion.Version.Major == 5 ? SystemColors.Control : Color.White;
                }

                //防Dock改变尺寸
                protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified) =>
                    base.SetBoundsCore(x, y, width, height, specified | BoundsSpecified.Size);

                /// <summary>
                /// 计算合适的消息区尺寸
                /// </summary>
                /// <param name="proposedSize">该参数此处定义为此控件可设置的最大尺寸</param>
                /// <remarks>该方法对太长的单行文本有做比例优化处理，避免用户摆头幅度过大扭到脖子</remarks>
                public override Size GetPreferredSize(Size proposedSize)
                {
                    if (proposedSize.Width < 10) { proposedSize.Width = int.MaxValue; }
                    if (proposedSize.Height < 10) { proposedSize.Height = int.MaxValue; }

                    var reservedWidth = Padding.Horizontal + (Icon?.Width + IconSpace ?? 0);

                    var wellSize = Size.Empty;
                    if (!string.IsNullOrEmpty(Text))
                    {
                        //优化文本块区块比例
                        var size = TextRenderer.MeasureText(Text, Font, new Size(proposedSize.Width - reservedWidth, 0), TEXTFLAGS);//用指定宽度测量文本面积
                        wellSize = Convert.ToSingle(size.Width) / size.Height > PreferredScale //过于宽扁的情况
                            ? Size.Ceiling(GetSameSizeWithNewScale(size, PreferredScale))
                            : size;

                        //凑齐整行高，确保尾行显示
                        var lineHeight = TextRenderer.MeasureText(" ", Font, new Size(int.MaxValue, 0), TEXTFLAGS).Height;//单行高，Font.Height不靠谱
                        var differ = wellSize.Height % lineHeight;
                        if (differ != 0)
                        {
                            wellSize.Height += lineHeight - differ;
                        }
                    }
                    if (Icon != null)
                    {
                        wellSize.Width += Icon.Width + IconSpace;
                        wellSize.Height = Math.Max(Icon.Height, wellSize.Height);
                    }
                    //System.Diagnostics.Debug.WriteLine(wellSize);
                    wellSize += Padding.Size;

                    //不应超过指定尺寸。宽度在上面已确保不会超过
                    if (wellSize.Height > proposedSize.Height) { wellSize.Height = proposedSize.Height; }

                    return wellSize;
                }

                /// <summary>
                /// 重绘
                /// </summary>
                protected override void OnPaint(PaintEventArgs e)
                {
                    var g = e.Graphics;
                    var rect = GetPaddedRectangle();

                    //绘制图标
                    if (Icon != null)
                    {
                        g.DrawIcon(Icon, Padding.Left, Padding.Top);

                        //右移文本区
                        rect.X += Icon.Width + IconSpace;
                        rect.Width -= Icon.Width + IconSpace;

                        //若文字太少，则与图标垂直居中
                        if (Text.Length < 100)
                        {
                            var textSize = TextRenderer.MeasureText(g, Text, Font, rect.Size, TEXTFLAGS);
                            if (textSize.Height <= Icon.Height)
                            {
                                rect.Y += (Icon.Height - textSize.Height) / 2;
                            }
                        }
                    }

                    //g.FillRectangle(Brushes.Gainsboro, rect);//test

                    //绘制文本
                    TextRenderer.DrawText(g, Text, Font, rect, SystemColors.WindowText, TEXTFLAGS);

                    //vista+ 系统绘制底部边线
                    if (Environment.OSVersion.Version.Major > 5)
                    {
                        using (var pen = new Pen(Color.FromArgb(223, 223, 223)))
                        {
                            var y = Height - 1;
                            g.DrawLine(pen, 0, y, Width, y);
                        }
                    }

                    base.OnPaint(e);
                }

                /// <summary>
                /// 根据原尺寸，得到相同面积、且指定比例的新尺寸
                /// </summary>
                /// <param name="src">原尺寸</param>
                /// <param name="scale">新尺寸比例。需是width/height</param>
                private static SizeF GetSameSizeWithNewScale(Size src, float scale)
                {
                    var sqr = src.Width * src.Height;//原面积
                    var w = Math.Sqrt(sqr * scale);//新面积宽
                    return new SizeF(Convert.ToSingle(w), Convert.ToSingle(sqr / w));
                }

                /// <summary>
                /// 获取刨去Padding的内容区
                /// </summary>
                private Rectangle GetPaddedRectangle()
                {
                    var r = ClientRectangle;
                    r.X += Padding.Left;
                    r.Y += Padding.Top;
                    r.Width -= Padding.Horizontal;
                    r.Height -= Padding.Vertical;
                    return r;
                }
            }

            /// <summary>
            /// 包装ToolBarButton为单一控件
            /// </summary>
            private class ToggleButton : Control
            {
                /// <summary>
                /// 展开/收起图标数据
                /// </summary>
                const string ImgDataBase64 =
@"iVBORw0KGgoAAAANSUhEUgAAACAAAAAQCAYAAAB3AH1ZAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJ
bWFnZVJlYWR5ccllPAAAA3NJREFUeNqklVlPFEEQx/8zPccue6gorMd6gBegeCAQD4w+oCx+AInx
IB4EfTK8+g2MQUUTcBU8En0wmvigEkyMxgcTjRrUqHFVUBRQQaJGl2WPmbG6dzCLWUiESf7T0739
666urqqVDjVcxT9PAWkfqZKUY491ktpIzaRXGPv5L15J+dZIRx26dqAwf56c48+Cx+1CzDDR//13
/seevvx3HZ8OxmLxMzSvjhT5Z+Nx8UoKfHOu31e+qWwZPBkOMBkwTAvRuAE21QuvJwNz5s6U25++
rv365dtC+4SxifJsfeVWvsCJ2TOzqyo2FsHt1OBSFeiqTItIsOhHw7JgGBZM+s72TcOvX+GccHgw
k7qttgHj5slOLNE0tXZNSQGYJEEhiDEJusLoW4ZMfZnGJVv0QmHhYuiaup+zE+W5Aftyc/xMURRh
acJIKpowqDVhkhu5LCspiY6k0OIL5s9mdrCNyp9sDKL+6PExeW5AwOebigRNiiVMkoFIPIFwlLcG
huIm4mRI3DRpAQg38oPMmD6Nuz4wGn+koRGH64/hxr1HuHjl2qg8D8JcZ4ZTRCtLSDjT1Ijz51rS
5lfVzj2o2rWXXCzDPcnNh3L5K5WntdHYdAqng6cwa/EK+AuK8SDUSx65gUAlxR1ZkcqLLDBpkJ+S
R8yOvbXw+vx4GOoZsXlZyQqsK10pNlDpjlVZDPMs0FL55mATLl04C39+EWblFf3l2zs+w7jZii1b
Kkfw3IDOcDiS5/G4yLjknQcCAbrPW3j8plvMWlu8XGwOsblMASYjFh3i3S4SS+W3Vddg++6apJ8t
OwN4HHH/p+G5AW3f+gbyvB632DwGHigSyjdvpn4b9ElZWF9aJE6uMAanJsOlK3jdNcAXuE2y0vEQ
rcXfyeCT0vPcES0funoNRTJpgixSRUQsLbapogIbVq8S47rKCORShQvbX7437NI6Km8Ol9sxeG7A
i2g0Fnz2PAQ3TcjQGBw02UGWOqig8L7bweB1qCSFxHD3/nMMDkWDnJ0oP1yK6z529y1i8ovydaVL
wXOaXxl3W7K4yKKykY/Rdq8dofe9d+x6jonyw6WYu+Pyj5/hzLedPcU61dDJLh1T3E4BRgYjCHV0
4/qdJ+bn/h+naW41KZpiwLh5Kc3fMS+vNXaRybVT7YMdcM2228d6/ov/I8AAPfkI7yO+mM8AAAAA
SUVORK5CYII=";

                readonly bool isToggleMode;
                bool isChecked;
                bool useAnimate;
                readonly ImageList imgList;

                /// <summary>
                /// Checked改变后
                /// </summary>
                public event EventHandler CheckedChanged;

                /// <summary>
                /// 使用动画按钮效果
                /// </summary>
                private bool UseAnimate
                {
                    get => useAnimate;
                    set
                    {
                        if (useAnimate == value) { return; }

                        useAnimate = value;
                        if (IsHandleCreated) { CreateHandle(); }
                    }
                }

                /// <summary>
                /// 获取或设置按钮是否处于按下状态
                /// </summary>
                [Description("获取或设置按钮是否处于按下状态"), DefaultValue(false)]
                public bool Checked
                {
                    get
                    {
                        if (IsHandleCreated)
                        {
                            //保证isChecked与实情吻合。TB_ISBUTTONCHECKED
                            isChecked = Convert.ToBoolean(SendMessage(Handle, 0x40A, IntPtr.Zero, IntPtr.Zero).ToInt32());
                        }
                        return isChecked;
                    }
                    set
                    {
                        if (isChecked == value || !isToggleMode) { return; }

                        isChecked = value;

                        if (IsHandleCreated)
                        {
                            //TB_CHECKBUTTON
                            SendMessage(Handle, 0x402, IntPtr.Zero, new IntPtr(Convert.ToInt32(value)));
                        }

                        OnCheckedChanged(EventArgs.Empty);
                    }
                }

                /// <summary>
                /// 创建ToolBarButtonControl
                /// </summary>
                public ToggleButton(bool useAnimate)
                {
                    SetStyle(ControlStyles.UserPaint, false);
                    SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                    SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                    SetStyle(ControlStyles.ResizeRedraw, true);

                    isToggleMode = true;//写死好了，独立版才提供设置
                    UseAnimate = useAnimate;

                    //将图标加入imageList
                    imgList = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
                    using (var ms = new MemoryStream(Convert.FromBase64String(ImgDataBase64)))
                    {
                        imgList.Images.AddStrip(Image.FromStream(ms));
                    }
                }

                /// <summary>
                /// 执行左键单击
                /// </summary>
                public void PerformClick()
                {
                    SendMessage(Handle, 0x201, new IntPtr(0x1), IntPtr.Zero);//WM_LBUTTONDOWN
                    Application.DoEvents();
                    SendMessage(Handle, 0x202, IntPtr.Zero, IntPtr.Zero);    //WM_LBUTTONUP
                }

                protected override void WndProc(ref Message m)
                {
                    //忽略鼠标双击消息，WM_LBUTTONDBLCLK
                    if (m.Msg == 0x203) { return; }

                    //有节操的响应鼠标动作
                    if ((m.Msg == 0x201 || m.Msg == 0x202) && (!Enabled || !Visible))
                    {
                        return;
                    }
                    base.WndProc(ref m);
                }

                //创建ToolBar
                protected override CreateParams CreateParams
                {
                    get
                    {
                        var prms = base.CreateParams;
                        prms.ClassName = "ToolbarWindow32";
                        prms.Style = 0x40000000
                            | 0x10000000
                            //| 0x2000000 //WS_CLIPCHILDREN
                            //| 0x8000
                            | 0x1
                            | 0x4
                            | 0x8
                            | 0x40
                            | 0x1000 //TBSTYLE_LIST，图标文本横排
                            ;
                        if (UseAnimate) { prms.Style |= 0x800; }//TBSTYLE_FLAT。flat模式在NT6.x下，按钮按下会有动画效果

                        prms.ExStyle = 0;

                        return prms;
                    }
                }

                protected override void OnHandleCreated(EventArgs e)
                {
                    base.OnHandleCreated(e);

                    //设置imgList
                    SendMessage(Handle, 0x430, IntPtr.Zero, imgList.Handle);//TB_SETIMAGELIST

                    //准备添加按钮
                    var btnStructSize = Marshal.SizeOf(typeof(TBBUTTON));
                    SendMessage(Handle, 0x41E, new IntPtr(btnStructSize), IntPtr.Zero);//TB_BUTTONSTRUCTSIZE，必须在添加按钮前

                    //构建按钮信息
                    var btnStruct = new TBBUTTON
                    {
                        //iBitmap = 0,
                        //idCommand = 0,
                        fsState = 0x4, //TBSTATE_ENABLED
                        iString = SendMessage(Handle, 0x44D, 0, Text + '\0')//TB_ADDSTRING
                    };
                    if (isToggleMode) { btnStruct.fsStyle = 0x2; }//BTNS_CHECK。作为切换按钮时

                    var btnStructStart = IntPtr.Zero;
                    try
                    {
                        btnStructStart = Marshal.AllocHGlobal(btnStructSize);//在非托管区创建一个指针
                        Marshal.StructureToPtr(btnStruct, btnStructStart, true);//把结构体塞到上述指针

                        //添加按钮
                        SendMessage(Handle, 0x444, new IntPtr(1)/*按钮数量*/, btnStructStart);//TB_ADDBUTTONS。从指针取按钮信息

                        //设置按钮尺寸刚好为ToolBar尺寸
                        AdjustButtonSize();
                    }
                    finally
                    {
                        if (btnStructStart != IntPtr.Zero) { Marshal.FreeHGlobal(btnStructStart); }
                    }
                }

                protected override bool ProcessCmdKey(ref Message m, Keys keyData)
                {
                    //将空格和回车作为鼠标单击处理
                    if (m.Msg == 0x100 && (keyData == Keys.Enter || keyData == Keys.Space))
                    {
                        PerformClick();
                        return true;
                    }

                    return base.ProcessCmdKey(ref m, keyData);
                }

                /// <summary>
                /// 处理助记键
                /// </summary>
                protected override bool ProcessMnemonic(char charCode)
                {
                    if (IsMnemonic(charCode, Text))
                    {
                        PerformClick();
                        return true;
                    }

                    return base.ProcessMnemonic(charCode);
                }

                protected override void OnClick(EventArgs e)
                {
                    //忽略鼠标右键
                    if (e is MouseEventArgs me && me.Button != MouseButtons.Left)
                    {
                        return;
                    }

                    //若是切换模式，直接引发Checked事件（不要通过设置Checked属性引发，因为OnClick发送之前就已经Check了）
                    //存在理论上的不可靠，但暂无更好办法
                    if (isToggleMode)
                    {
                        OnCheckedChanged(EventArgs.Empty);
                    }

                    base.OnClick(e);
                }

                //重绘后重设按钮尺寸
                protected override void OnInvalidated(InvalidateEventArgs e)
                {
                    base.OnInvalidated(e);
                    AdjustButtonSize();
                }

                /// <summary>
                /// 引发CheckedChanged事件
                /// </summary>
                protected virtual void OnCheckedChanged(EventArgs e)
                {
                    SetImageIndex(Checked ? 1 : 0);
                    CheckedChanged?.Invoke(this, e);
                }

                /// <summary>
                /// 设置图标索引
                /// </summary>
                private void SetImageIndex(int index)
                {
                    //TB_CHANGEBITMAP
                    SendMessage(Handle, 0x42B, IntPtr.Zero, new IntPtr(index));
                }

                /// <summary>
                /// 调整按钮尺寸刚好为ToolBar尺寸
                /// </summary>
                private void AdjustButtonSize()
                {
                    var lParam = new IntPtr((Width & 0xFFFF) | (Height << 0x10)); //MakeLParam手法
                    SendMessage(Handle, 0x41F, IntPtr.Zero, lParam); //TB_SETBUTTONSIZE
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        if (IsHandleCreated)
                        {
                            SendMessage(Handle, 0x400 + 22, IntPtr.Zero, IntPtr.Zero); //TB_DELETEBUTTON
                        }
                        imgList?.Dispose();
                    }
                    base.Dispose(disposing);
                }
            }

            #region Win32 API

            // ReSharper disable MemberCanBePrivate.Local
            // ReSharper disable FieldCanBeMadeReadOnly.Local

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

            [DllImport("winmm.dll", CharSet = CharSet.Auto)]
            static extern bool PlaySound([MarshalAs(UnmanagedType.LPWStr)] string soundName, IntPtr hmod, int soundFlags);

            [StructLayout(LayoutKind.Sequential)]
            private struct TBBUTTON
            {
                public int iBitmap;
                public int idCommand;
                public byte fsState;
                public byte fsStyle;
                public byte bReserved0;
                public byte bReserved1;
                public IntPtr dwData;
                public IntPtr iString;
            }

            // ReSharper restore FieldCanBeMadeReadOnly.Local
            // ReSharper restore MemberCanBePrivate.Local

            #endregion

            #endregion
        }
    }
}
