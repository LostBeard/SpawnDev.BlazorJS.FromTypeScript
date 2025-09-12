using Radzen;
using SpawnDev.BlazorJS.FromTypeScript.Components;

namespace SpawnDev.BlazorJS.FromTypeScript.Services
{
    public class InputBoxOptions
    {
        public string Text { get; set; } = "";
        public string PlaceHolder { get; set; } = "";
        public string DefaultValue { get; set; } = "";
        public bool Required { get; set; } = false;
        public string RequiredError { get; set; } = null;
        public string Regex { get; set; } = null;
        public string RegexError { get; set; } = null;
        public string SubmitText { get; set; } = "OK";
        public bool PopupErrors { get; set; } = false;
    }
    public static class DialogServiceExtensions
    {
        public static async Task<string?> ShowInputBox(this DialogService _this, string title, InputBoxOptions inputBoxOptions, DialogOptions? options = null)
        {
            var parameters = new Dictionary<string, object>() {
                { "InputBoxOptions", inputBoxOptions },
            };
            try
            {
                return await _this.OpenAsync<InputBox>(title, parameters, options);
            }
            catch { }
            return null;
        }

        public static Task<string?> ShowInputBox(this DialogService _this, string title, string text = "", string placeHolder = "", string defaultValue = "", DialogOptions? options = null)
        {
            var inputBoxOptions = new InputBoxOptions();
            inputBoxOptions.Text = text;
            inputBoxOptions.PlaceHolder = placeHolder;
            inputBoxOptions.DefaultValue = defaultValue;
            return _this.ShowInputBox(title, inputBoxOptions, options);
        }
    }
}
