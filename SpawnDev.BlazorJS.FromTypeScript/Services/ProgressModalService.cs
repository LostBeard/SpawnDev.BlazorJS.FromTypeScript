namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class ProgressModalService
    {
        public event Action OnChanged = default!;
        public IReadOnlyList<ProgressSession> Sessions => _Sessions.ToList();
        List<ProgressSession> _Sessions { get; } = new List<ProgressSession>();
        public bool Visible => Sessions.Any();
        public async Task<ProgressSession> NewSession(string text = "")
        {
            var session = new ProgressSession();
            session.Text = text ?? "";
            session.OnChanged += Session_OnChanged;
            _Sessions.Add(session);
            StateHasChanged();
            await Task.Delay(20);
            return session;
        }
        void StateHasChanged()
        {
            OnChanged?.Invoke();
        }
        private void Session_OnChanged(ProgressSession session)
        {
            if (session.Disposed)
            {
                _Sessions.Remove(session);
                session.OnChanged -= Session_OnChanged;
            }
            StateHasChanged();
        }
        public sealed class ProgressSession : IDisposable
        {
            public event Action<ProgressSession> OnChanged = default!;
            bool _ShowValue = true;
            public bool ShowValue
            {
                get => _ShowValue;
                set
                {
                    if (_ShowValue == value) return;
                    _ShowValue = value;
                    StateHasChanged();
                }
            }
            bool _Indeterminate = true;
            public bool Indeterminate
            {
                get => _Indeterminate;
                set
                {
                    if (_Indeterminate == value) return;
                    _Indeterminate = value;
                    StateHasChanged();
                }
            }
            string _Text = "";
            public string Text
            {
                get => _Text;
                set
                {
                    if (_Text == value) return;
                    _Text = value;
                    StateHasChanged();
                }
            }
            string _Unit = "";
            public string Unit
            {
                get => _Unit;
                set
                {
                    if (_Unit == value) return;
                    _Unit = value;
                    StateHasChanged();
                }
            }
            double _Value;
            public double Value
            {
                get => _Value;
                set
                {
                    Indeterminate = false;
                    if (_Value == value) return;
                    _Value = value;
                    StateHasChanged();
                }
            }
            double _Min = 0;
            public double Min
            {
                get => _Min;
                set
                {
                    if (_Min == value) return;
                    _Min = value;
                    StateHasChanged();
                }
            }
            double _Max = 100;
            public double Max
            {
                get => _Max;
                set
                {
                    if (_Max == value) return;
                    _Max = value;
                    StateHasChanged();
                }
            }
            void StateHasChanged()
            {
                OnChanged?.Invoke(this);
            }
            public bool Disposed { get; private set; }
            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                GC.SuppressFinalize(this);
                Dispose(true);
            }
            void Dispose(bool disposing)
            {
                StateHasChanged();
            }
            ~ProgressSession() => Dispose(false);
        }
    }

}
