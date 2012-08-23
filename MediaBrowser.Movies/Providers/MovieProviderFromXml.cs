﻿using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Xml;
using MediaBrowser.Model.Entities;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Providers
{
    [Export(typeof(BaseMetadataProvider))]
    public class MovieProviderFromXml : BaseMetadataProvider
    {
        public override bool Supports(BaseEntity item)
        {
            return item is Movie;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        public override async Task FetchAsync(BaseEntity item, ItemResolveEventArgs args)
        {
            await Task.Run(() => { Fetch(item, args); }).ConfigureAwait(false);
        }

        private void Fetch(BaseEntity item, ItemResolveEventArgs args)
        {
            if (args.ContainsFile("movie.xml"))
            {
                new BaseItemXmlParser<Movie>().Fetch(item as Movie, Path.Combine(args.Path, "movie.xml"));
            }
        }
    }
}
