﻿using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.TV
{
    /// <summary>
    /// Class Season
    /// </summary>
    public class Season : Folder, IHasSeries, IHasLookupInfo<SeasonInfo>
    {

        /// <summary>
        /// Seasons are just containers
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IncludeInIndex
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override BaseItem DisplayParent
        {
            get { return Series ?? Parent; }
        }

        /// <summary>
        /// We want to group into our Series
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool GroupInIndex
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public override Folder IndexContainer
        {
            get
            {
                return Series;
            }
        }

        // Genre, Rating and Stuido will all be the same
        protected override IEnumerable<string> GetIndexByOptions()
        {
            return new List<string> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref")}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref")},
                {LocalizedStrings.Instance.GetString("DirectorDispPref")},
                {LocalizedStrings.Instance.GetString("YearDispPref")},
            };
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateUserDataKey()
        {
            if (Series != null)
            {
                var seasonNo = IndexNumber ?? 0;
                return Series.GetUserDataKey() + seasonNo.ToString("000");
            }

            return base.CreateUserDataKey();
        }

        /// <summary>
        /// The _series
        /// </summary>
        private Series _series;
        /// <summary>
        /// This Episode's Series Instance
        /// </summary>
        /// <value>The series.</value>
        [IgnoreDataMember]
        public Series Series
        {
            get { return _series ?? (_series = FindParent<Series>()); }
        }

        [IgnoreDataMember]
        public string SeriesPath
        {
            get
            {
                var series = Series;

                if (series != null)
                {
                    return series.Path;
                }

                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        /// <summary>
        /// Our rating comes from our series
        /// </summary>
        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get
            {
                var series = Series;
                return series != null ? series.OfficialRatingForComparison : base.OfficialRatingForComparison;
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateSortName()
        {
            return IndexNumber != null ? IndexNumber.Value.ToString("0000") : Name;
        }

        [IgnoreDataMember]
        public bool IsMissingSeason
        {
            get { return LocationType == LocationType.Virtual && GetEpisodes().All(i => i.IsMissingEpisode); }
        }

        [IgnoreDataMember]
        public bool IsUnaired
        {
            get { return GetEpisodes().All(i => i.IsUnaired); }
        }

        [IgnoreDataMember]
        public bool IsVirtualUnaired
        {
            get { return LocationType == LocationType.Virtual && IsUnaired; }
        }

        [IgnoreDataMember]
        public bool IsMissingOrVirtualUnaired
        {
            get { return LocationType == LocationType.Virtual && GetEpisodes().All(i => i.IsVirtualUnaired || i.IsMissingEpisode); }
        }

        [IgnoreDataMember]
        public bool IsSpecialSeason
        {
            get { return (IndexNumber ?? -1) == 0; }
        }

        /// <summary>
        /// Gets the episodes.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{Episode}.</returns>
        public IEnumerable<Episode> GetEpisodes(User user)
        {
            var config = user.Configuration;

            return GetEpisodes(user, config.DisplayMissingEpisodes, config.DisplayUnairedEpisodes);
        }

        public IEnumerable<Episode> GetEpisodes(User user, bool includeMissingEpisodes, bool includeVirtualUnairedEpisodes)
        {
            var episodes = GetRecursiveChildren(user)
                .OfType<Episode>();

            var series = Series;

            if (IndexNumber.HasValue && series != null)
            {
                return series.GetEpisodes(user, IndexNumber.Value, includeMissingEpisodes, includeVirtualUnairedEpisodes, episodes);
            }

            if (series != null && series.ContainsEpisodesWithoutSeasonFolders)
            {
                var seasonNumber = IndexNumber;
                var list = episodes.ToList();

                if (seasonNumber.HasValue)
                {
                    list.AddRange(series.GetRecursiveChildren(user).OfType<Episode>()
                        .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == seasonNumber.Value));
                }
                else
                {
                    list.AddRange(series.GetRecursiveChildren(user).OfType<Episode>()
                        .Where(i => !i.ParentIndexNumber.HasValue));
                }

                episodes = list.DistinctBy(i => i.Id);
            }
            
            if (!includeMissingEpisodes)
            {
                episodes = episodes.Where(i => !i.IsMissingEpisode);
            }
            if (!includeVirtualUnairedEpisodes)
            {
                episodes = episodes.Where(i => !i.IsVirtualUnaired);
            }

            return LibraryManager
                .Sort(episodes, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending)
                .Cast<Episode>();
        }

        private IEnumerable<Episode> GetEpisodes()
        {
            var episodes = RecursiveChildren.OfType<Episode>();
            var series = Series;

            if (series != null && series.ContainsEpisodesWithoutSeasonFolders)
            {
                var seasonNumber = IndexNumber;
                var list = episodes.ToList();

                if (seasonNumber.HasValue)
                {
                    list.AddRange(series.RecursiveChildren.OfType<Episode>()
                        .Where(i => i.ParentIndexNumber.HasValue && i.ParentIndexNumber.Value == seasonNumber.Value));
                }
                else
                {
                    list.AddRange(series.RecursiveChildren.OfType<Episode>()
                        .Where(i => !i.ParentIndexNumber.HasValue));
                }

                episodes = list.DistinctBy(i => i.Id);
            }

            return episodes;
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetEpisodes(user);
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block. Let either the entire series rating or episode rating determine it
            return false;
        }

        [IgnoreDataMember]
        public string SeriesName
        {
            get
            {
                var series = Series;
                return series == null ? null : series.Name;
            }
        }

        /// <summary>
        /// Gets the lookup information.
        /// </summary>
        /// <returns>SeasonInfo.</returns>
        public SeasonInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<SeasonInfo>();

            var series = Series;

            if (series != null)
            {
                id.SeriesProviderIds = series.ProviderIds;
                id.AnimeSeriesIndex = series.AnimeSeriesIndex;
            }

            return id;
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            var locationType = LocationType;

            if (locationType == LocationType.FileSystem || locationType == LocationType.Offline)
            {
                if (!IndexNumber.HasValue && !string.IsNullOrEmpty(Path))
                {
                    IndexNumber = IndexNumber ?? LibraryManager.GetSeasonNumberFromPath(Path);

                    // If a change was made record it
                    if (IndexNumber.HasValue)
                    {
                        hasChanges = true;
                    }
                }
            }

            return hasChanges;
        }
    }
}
