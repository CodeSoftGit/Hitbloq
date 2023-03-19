﻿using System;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace Hitbloq.UI
{
	internal class MenuButtonUI : IInitializable, IDisposable
	{
		private readonly HitbloqFlowCoordinator _hitbloqFlowCoordinator;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly MenuButton _menuButton;

		public MenuButtonUI(MainFlowCoordinator mainFlowCoordinator, HitbloqFlowCoordinator hitbloqFlowCoordinator)
		{
			_menuButton = new MenuButton("Hitbloq", "Browse Pools & More!", MenuButtonClicked);
			_mainFlowCoordinator = mainFlowCoordinator;
			_hitbloqFlowCoordinator = hitbloqFlowCoordinator;
		}

		public void Dispose()
		{
			if (MenuButtons.IsSingletonAvailable)
			{
				MenuButtons.instance.UnregisterButton(_menuButton);
			}
		}

		public void Initialize()
		{
			MenuButtons.instance.RegisterButton(_menuButton);
		}

		private void MenuButtonClicked()
		{
			_hitbloqFlowCoordinator.Show();
		}
	}
}