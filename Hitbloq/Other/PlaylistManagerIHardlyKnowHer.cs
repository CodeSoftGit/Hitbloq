﻿using HMUI;
using IPA.Utilities;
using SiraUtil.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace Hitbloq.Other
{
    internal class PlaylistManagerIHardlyKnowHer : IInitializable, IDisposable
    {
        private readonly IHttpService siraHttpService;
        private readonly LevelFilteringNavigationController levelFilteringNavigationController;
        private readonly SelectLevelCategoryViewController selectLevelCategoryViewController;
        private readonly IconSegmentedControl levelCategorySegmentedControl;

        private CancellationTokenSource? tokenSource;

        public bool IsDownloading => tokenSource is {IsCancellationRequested: false};
        public event Action<string>? HitbloqPlaylistSelected;

        public PlaylistManagerIHardlyKnowHer(IHttpService siraHttpService, LevelFilteringNavigationController levelFilteringNavigationController, SelectLevelCategoryViewController selectLevelCategoryViewController)
        {
            this.siraHttpService = siraHttpService;
            this.levelFilteringNavigationController = levelFilteringNavigationController;
            this.selectLevelCategoryViewController = selectLevelCategoryViewController;
            levelCategorySegmentedControl = selectLevelCategoryViewController.GetField<IconSegmentedControl, SelectLevelCategoryViewController>("_levelFilterCategoryIconSegmentedControl");
        }

        public void Initialize()
        {
            PlaylistManager.Utilities.Events.playlistSelected += OnPlaylistSelected;
        }

        public void Dispose()
        {
            PlaylistManager.Utilities.Events.playlistSelected -= OnPlaylistSelected;
        }

        internal void OpenPlaylist(string poolID, Action? onDownloadComplete = null)
            => _ = OpenPlaylistAsync(poolID, onDownloadComplete);

        private async Task OpenPlaylistAsync(string poolID, Action? onDownloadComplete = null)
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            var playlistToSelect = await GetPlaylist(poolID, tokenSource.Token);

            if (playlistToSelect == null)
            {
                return;
            }

            await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                onDownloadComplete?.Invoke();
                levelCategorySegmentedControl.SelectCellWithNumber(1);
                selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(levelCategorySegmentedControl, 1);
                levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(playlistToSelect);
            });

            tokenSource.Dispose();
            tokenSource = null;
        }

        internal void CancelDownload() => tokenSource?.Cancel();

        private async Task<IBeatmapLevelPack?> GetPlaylist(string poolID, CancellationToken token = default)
        {
            var syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";

            var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true).ToList();
            foreach (var playlist in playlists)
            {
                if (playlist.TryGetCustomData("syncURL", out var url) && url is string urlString)
                {
                    if (urlString == syncURL)
                    {
                        return playlist;
                    }
                }
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync(syncURL, cancellationToken: token).ConfigureAwait(false);
                Stream playlistStream = new MemoryStream(await webResponse.ReadAsByteArrayAsync());
                var newPlaylist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler?.Deserialize(playlistStream);

                if (newPlaylist != null)
                {
                    var playlistManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("Hitbloq");
                    playlistManager.StorePlaylist(newPlaylist);   
                }
                
                return newPlaylist;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        private void OnPlaylistSelected(BeatSaberPlaylistsLib.Types.IPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
        {
            if (playlist.TryGetCustomData("syncURL", out var url) && url is string urlString)
            {
                if (urlString.Contains("https://hitbloq.com/static/hashlists/"))
                {
                    var pool = urlString.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
                    if (!string.IsNullOrEmpty(pool))
                    {
                        HitbloqPlaylistSelected?.Invoke(pool!);
                    }
                }
            }
        }
    }
}
