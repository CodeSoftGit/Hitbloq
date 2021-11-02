﻿using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class RankInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly UserIDSource userIDSource;

        public RankInfoSource(SiraClient siraClient, UserIDSource userIDSource)
        {
            this.siraClient = siraClient;
            this.userIDSource = userIDSource;
        }

        public async Task<HitbloqRankInfo> GetRankInfoForSelfAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID.id != -1)
            {
                return await GetRankInfoAsync(poolID, userID.id, cancellationToken);
            }
            return null;
        }

        public async Task<HitbloqRankInfo> GetRankInfoAsync(string poolID, int userID, CancellationToken? cancellationToken = null)
        {
            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userID}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                return Utils.ParseWebResponse<HitbloqRankInfo>(webResponse);
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}
