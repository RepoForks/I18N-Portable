﻿using System.Linq;
using System.Threading.Tasks;
using I18NPortable;
using Xamarin.Forms;

namespace Sample.Forms.Core.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private Command _changeLangCommand;
        
        public Command ChangeLanguageCommand => _changeLangCommand ?? (_changeLangCommand = new Command(async () => await ChangeLanguage()));

        #warning Do not do this
        public Page CurrentPage { get; set; }

        private async Task ChangeLanguage()
        {
            var cancel = "Cancel".Translate();
            var result = await CurrentPage.DisplayActionSheet(Strings["ChooseLanguage"], cancel, null,
                LanguagesToSelect.Select(l => l.DisplayName).ToArray());

            if (result == cancel)
                return;

            var resultLocale = LanguagesToSelect.Single(l => l.DisplayName == result).Locale;

            LoadLocale(resultLocale);
        }
    }
}