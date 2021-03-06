﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Grabacr07.KanColleViewer.Composition;
using Grabacr07.KanColleViewer.Models;
using Grabacr07.KanColleViewer.Models.Settings;
using Grabacr07.KanColleViewer.Properties;
using Grabacr07.KanColleViewer.ViewModels.Composition;
using Grabacr07.KanColleWrapper;
using Grabacr07.KanColleWrapper.Models;
using Livet;
using Livet.EventListeners;
using MetroTrilithon.Mvvm;

namespace Grabacr07.KanColleViewer.ViewModels.Settings
{
	public class SettingsViewModel : TabItemViewModel
	{
		public static SettingsViewModel Instance { get; } = new SettingsViewModel();


		public override string Name
		{
			get { return Resources.Settings; }
			protected set { throw new NotImplementedException(); }
		}


		public ScreenshotSettingsViewModel ScreenshotSettings { get; }

		public WindowSettingsViewModel WindowSettings { get; }

		public NetworkSettingsViewModel NetworkSettings { get; }

		public UserStyleSheetSettingsViewModel UserStyleSheetSettings { get; }

		public NavigatorViewModel Navigator { get; set; }

		public BrowserZoomFactor BrowserZoomFactor { get; }

		public IReadOnlyCollection<CultureViewModel> Cultures { get; }

		public IReadOnlyCollection<BindableTextViewModel> Libraries { get; }

		public List<PluginViewModel> LoadedPlugins { get; }

		public List<LoadFailedPluginViewModel> FailedPlugins { get; }

		#region ViewRangeSettingsCollection 変更通知プロパティ

		private List<ViewRangeSettingsViewModel> _ViewRangeSettingsCollection;

		public List<ViewRangeSettingsViewModel> ViewRangeSettingsCollection
		{
			get { return this._ViewRangeSettingsCollection; }
			set
			{
				if (this._ViewRangeSettingsCollection != value)
				{
					this._ViewRangeSettingsCollection = value;
					this.RaisePropertyChanged();
				}
			}
		}

		#endregion

		private SettingsViewModel()
		{
			this.ScreenshotSettings = new ScreenshotSettingsViewModel().AddTo(this);
			this.WindowSettings = new WindowSettingsViewModel().AddTo(this);
			this.NetworkSettings = new NetworkSettingsViewModel().AddTo(this);
			this.UserStyleSheetSettings = new UserStyleSheetSettingsViewModel().AddTo(this);

			this.BrowserZoomFactor = new BrowserZoomFactor { Current = GeneralSettings.BrowserZoomFactor };
			this.BrowserZoomFactor
				.Subscribe(nameof(this.BrowserZoomFactor.Current), () => GeneralSettings.BrowserZoomFactor.Value = this.BrowserZoomFactor.Current)
				.AddTo(this);
			GeneralSettings.BrowserZoomFactor.Subscribe(x => this.BrowserZoomFactor.Current = x).AddTo(this);

			this.Cultures = new[] { new CultureViewModel { DisplayName = "(auto)" } }
				.Concat(ResourceService.Current.SupportedCultures
					.Select(x => new CultureViewModel { DisplayName = x.EnglishName, Name = x.Name })
					.OrderBy(x => x.DisplayName))
				.ToList();

			this.Libraries = ProductInfo.Libraries.Aggregate(
				new List<BindableTextViewModel>(),
				(list, lib) =>
				{
					list.Add(new BindableTextViewModel { Text = list.Count == 0 ? "Built with " : ", " });
					list.Add(new HyperlinkViewModel { Text = lib.Name.Replace(' ', Convert.ToChar(160)), Uri = lib.Url });
					// ☝プロダクト名の途中で改行されないように、space を non-break space に置き換えてあげてるんだからねっっ
					return list;
				});

			this.CompositeDisposable.Add(new PropertyChangedEventListener(KanColleClient.Current.Translations)
			{
				(sender, args) => this.RaisePropertyChanged(args.PropertyName),
			});

			this.ViewRangeSettingsCollection = ViewRangeCalcLogic.Logics
				.Select(x => new ViewRangeSettingsViewModel(x))
				.ToList();

			this.LoadedPlugins = new List<PluginViewModel>(
				PluginService.Current.Plugins.Select(x => new PluginViewModel(x)));

			this.FailedPlugins = new List<LoadFailedPluginViewModel>(
				PluginService.Current.FailedPlugins.Select(x => new LoadFailedPluginViewModel(x)));
		}


		public void Initialize()
		{
			this.WindowSettings.Initialize();
			this.NetworkSettings.Initialize();
			this.UserStyleSheetSettings.Initialize();
		}


		public class ViewRangeSettingsViewModel : ViewModel
		{
			private bool selected;

			public ICalcViewRange Logic { get; set; }

			public string Name => GetLocalisedStrings(Logic.Id);

			public string Description => GetLocalisedStrings(Logic.Id, true);

			public bool Selected
			{
				get { return this.selected; }
				set
				{
					this.selected = value;
					if (value)
					{
						KanColleSettings.ViewRangeCalcType.Value = this.Logic.Id;
					}
				}
			}

			public ViewRangeSettingsViewModel(ICalcViewRange logic)
			{
				this.Logic = logic;
				this.selected = KanColleSettings.ViewRangeCalcType == logic.Id;
				ResourceService.Current.Subscribe(x =>
				{
					this.RaisePropertyChanged(nameof(Name));
					this.RaisePropertyChanged(nameof(Description));
				});
			}

			private string GetLocalisedStrings(string type, bool desc = false)
			{
				switch (type)
				{
					case "KanColleViewer.Type1":
						return !desc ? Resources.ViewRangeLogic_Type1_Name : Resources.ViewRangeLogic_Type1_Desc;
					case "KanColleViewer.Type2":
						return !desc ? Resources.ViewRangeLogic_Type2_Name : Resources.ViewRangeLogic_Type2_Desc;
					case "KanColleViewer.Type3":
						return !desc ? Resources.ViewRangeLogic_Type3_Name : Resources.ViewRangeLogic_Type3_Desc;
				}

				return !desc ? Logic.Name : Logic.Description;
			}
		}
	}
}
