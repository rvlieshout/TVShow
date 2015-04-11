﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;
using TVShow.Helpers;
using TVShow.Model.Movie;
using System.Xml;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TvShow.Helpers;
using System.Xml.Serialization;
using TVShow.Model.Cast;

namespace TVShow.Model.Api
{
    /// <summary>
    /// Request data from YTS API
    /// </summary>
    public class Service : IService
    {
        #region Methods
        #region Method -> GetMoviesInfosAsync
        /// <summary>
        /// Request data from YTS API
        /// </summary>
        /// <param name="searchParameter">searchParameter</param>
        /// <param name="MaxMoviesPerPage">MaxMoviesPerPage</param>
        /// <param name="pageNumberToLoad">pageNumberToLoad</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<IEnumerable<MovieShortDetails>, IEnumerable<Exception>>> GetMoviesInfosAsync(string searchParameter, 
            int maxMoviesPerPage,
            int pageNumberToLoad, 
            CancellationTokenSource cancellationToken)
        {
            var client = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "list_movies.json");
            request.AddParameter("limit", maxMoviesPerPage);
            request.AddParameter("page", pageNumberToLoad);
            if (String.IsNullOrEmpty(searchParameter))
            {
                request.AddParameter("sort_by", "like_count");
            }
            else
            {
                request.AddParameter("query_term", searchParameter);
            }
            List<Exception> ex = new List<Exception>();
            WrapperMovieShortDetails results = new WrapperMovieShortDetails();
            try
            {
                IRestResponse response = await client.ExecuteTaskAsync(request, cancellationToken.Token);
                results =
                JsonConvert.DeserializeObject<WrapperMovieShortDetails>(response.Content);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }
            catch (Exception e)
            {
                ex.Add(e);
            }

            if (results != null && results.Data != null && results.Data.Movies != null)
            {
                return new Tuple<IEnumerable<MovieShortDetails>,IEnumerable<Exception>>(results.Data.Movies, ex);
            }
            else
            {
                return new Tuple<IEnumerable<MovieShortDetails>,IEnumerable<Exception>>(null, ex);
            }
        }
        #endregion

        #region Method -> GetMovieInfosAsync
        /// <summary>
        /// Request data from OMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="movieId">movieId</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<MovieFullDetails, IEnumerable<Exception>>> GetMovieInfosAsync(int movieId, 
            CancellationTokenSource cancellationToken)
        {
            var restClient = new RestClient(Constants.YtsApiEndpoint);
            var request = new RestRequest("/{segment}", Method.GET);
            request.AddUrlSegment("segment", "movie_details.json");

            request.AddParameter("movie_id", movieId);
            request.AddParameter("with_images", true);
            request.AddParameter("with_cast", true);

            List<Exception> ex = new List<Exception>();
            WrapperMovieFullDetails responseWrapper = new WrapperMovieFullDetails();
            try
            {
                IRestResponse response = await restClient.ExecuteTaskAsync(request, cancellationToken.Token);
                if (response.StatusCode != HttpStatusCode.InternalServerError)
                {
                    responseWrapper =
                        JsonConvert.DeserializeObject<WrapperMovieFullDetails>(response.Content);
                }
                else
                {
                    ex.Add(new Exception());
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (responseWrapper != null && responseWrapper.Movie != null)
            {
                return new Tuple<MovieFullDetails, IEnumerable<Exception>>(responseWrapper.Movie, ex);
            }
            else
            {
                return new Tuple<MovieFullDetails, IEnumerable<Exception>>(null, ex);
            }
        }
        #endregion

        #region Method -> DownloadMovieTorrent
        /// <summary>
        /// Request data from OMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="imdbCode">imdbCode</param>
        /// <param name="torentUrl">movieId</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadMovieTorrent(string imdbCode, string torentUrl,
            CancellationTokenSource cancellationToken)
        {
            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> torrentFile = new Tuple<string, Exception>(String.Empty, new Exception());

            try
            {
                torrentFile =
                    await
                        DownloadFile(imdbCode,
                            new Uri(torentUrl), Constants.FileType.TorrentFile,
                            cancellationToken.Token);

                if (torrentFile.Item2 == null)
                {
                    torrentFile = new Tuple<string, Exception>(Constants.TorrentDirectory +
                                                              imdbCode +
                                                              ".torrent", new Exception());
                }
                else
                {
                    ex.Add(torrentFile.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (torrentFile != null && torrentFile.Item1 != null)
            {
                return new Tuple<string,IEnumerable<Exception>>(torrentFile.Item1, ex);
            }
            else
            {
                return new Tuple<string,IEnumerable<Exception>>(null, ex);
            }
        }
        #endregion

        #region Method -> DownloadMovieCoverAsync

        /// <summary>
        /// Request data from TMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="imdbCode">movie</param>
        /// <param name="imageUrl">imageUrl</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadMovieCoverAsync(string imdbCode,
            string imageUrl,
            CancellationTokenSource cancellationToken)
        {
            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> coverImage = new Tuple<string, Exception>(String.Empty, new Exception());

            try
            {
                coverImage =
                    await
                        DownloadFile(imdbCode,
                            new Uri(imageUrl), Constants.FileType.CoverImage,
                            cancellationToken.Token);

                if (coverImage.Item2 == null)
                {
                    coverImage = new Tuple<string, Exception>(Constants.CoverMoviesDirectory +
                                                              imdbCode +
                                                              ".jpg", new Exception());
                }
                else
                {
                    ex.Add(coverImage.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (coverImage != null && coverImage.Item1 != null)
            {
                return new Tuple<string, IEnumerable<Exception>>(coverImage.Item1, ex);
            }
            else
            {
                return new Tuple<string,IEnumerable<Exception>>(null, ex);
            }
        }
        #endregion

        #region Method -> DownloadMoviePosterAsync

        /// <summary>
        /// Request data from TMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="imdbCode">movie</param>
        /// <param name="imageUrl">imageUrl</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadMoviePosterAsync(string imdbCode,
            string imageUrl,
            CancellationTokenSource cancellationToken)
        {
            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> posterImage = new Tuple<string,Exception>(String.Empty, new Exception());

            try
            {
                posterImage =
                    await
                        DownloadFile(imdbCode,
                            new Uri(imageUrl), Constants.FileType.PosterImage,
                            cancellationToken.Token);

                if (posterImage.Item2 == null)
                {
                    posterImage = new Tuple<string, Exception>(Constants.PosterMovieDirectory +
                                                              imdbCode +
                                                              ".jpg", new Exception());
                }
                else
                {
                    ex.Add(posterImage.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (posterImage != null && posterImage.Item1 != null)
            {
                return new Tuple<string,IEnumerable<Exception>>(posterImage.Item1, ex);
            }
            else
            {
                return new Tuple<string,IEnumerable<Exception>>(null, ex);
            }
        }
        #endregion

        #region Method -> DownloadDirectorsImagesAsync

        /// <summary>
        /// Request data from TMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="name">movie</param>
        /// <param name="imageUrl">imageUrl</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadDirectorsImagesAsync(string name,
            string imageUrl,
            CancellationTokenSource cancellationToken)
        {
            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> directorImage = new Tuple<string, Exception>(String.Empty, new Exception());

            try
            {
                directorImage =
                    await
                        DownloadFile(name,
                            new Uri(imageUrl), Constants.FileType.DirectorImage,
                            cancellationToken.Token);

                if (directorImage.Item2 == null)
                {
                    directorImage = new Tuple<string, Exception>(Constants.DirectorMovieDirectory +
                                                              name +
                                                              ".jpg", new Exception());
                }
                else
                {
                    ex.Add(directorImage.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (directorImage != null && directorImage.Item1 != null)
            {
                return new Tuple<string,IEnumerable<Exception>>(directorImage.Item1, ex);
            }
            else
            {
                return new Tuple<string,IEnumerable<Exception>>(null, ex);
            }
        }

        #endregion

        #region Method -> DownloadActorsImageAsync

        /// <summary>
        /// Request data from TMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="imdbCode">movie</param>
        /// <param name="imageUrl">imageUrl</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadActorsImagesAsync(string name,
            string imageUrl,
            CancellationTokenSource cancellationToken)
        {
            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> actorImage = new Tuple<string, Exception>(String.Empty, new Exception());

            try
            {
                actorImage =
                    await
                        DownloadFile(name,
                            new Uri(imageUrl), Constants.FileType.ActorImage,
                            cancellationToken.Token);

                if (actorImage.Item2 == null)
                {
                    actorImage = new Tuple<string, Exception>(Constants.ActorMovieDirectory +
                                                              name +
                                                              ".jpg", new Exception());
                }
                else
                {
                    ex.Add(actorImage.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            if (actorImage != null && actorImage.Item1 != null)
            {
                return new Tuple<string,IEnumerable<Exception>>(actorImage.Item1, ex);
            }
            else
            {
                return new Tuple<string,IEnumerable<Exception>>(null, ex);
            }
        }

        #endregion

        #region Method -> DownloadMovieBackgroundImage

        /// <summary>
        /// Request data from TMDb API
        /// </summary>
        /// <param name="callback">callback</param>
        /// <param name="imdbCode">movie</param>
        /// <param name="cancellationToken">cancellationToken</param>
        public async Task<Tuple<string, IEnumerable<Exception>>> DownloadMovieBackgroundImage(string imdbCode,
            CancellationTokenSource cancellationToken)
        {
            TMDbClient tmDbclient = new TMDbClient("52db02421219a8b6b4a8eed1df0b8bd8");
            tmDbclient.GetConfig();
            TMDbLib.Objects.Movies.Movie movie = tmDbclient.GetMovie(imdbCode, MovieMethods.Images);
            Uri imageUri = tmDbclient.GetImageUrl("original",
                movie.Images.Backdrops.Aggregate((i1, i2) => i1.VoteAverage > i2.VoteAverage ? i1 : i2).FilePath);

            List<Exception> ex = new List<Exception>();
            Tuple<string, Exception> res;
            string BackgroundImage = String.Empty;
            try
            {
                res =
                    await DownloadFile(imdbCode, imageUri, Constants.FileType.BackgroundImage, cancellationToken.Token);
                if (res.Item2 == null)
                {
                    BackgroundImage = Constants.BackgroundMovieDirectory + imdbCode + ".jpg";
                }
                else
                {
                    ex.Add(res.Item2);
                }
            }
            catch (WebException webException)
            {
                ex.Add(webException);
            }
            catch (TaskCanceledException e)
            {
                ex.Add(e);
            }

            return new Tuple<string,IEnumerable<Exception>>(BackgroundImage, ex);
        }

        #endregion

        #region Method -> DownloadFile
        /// <summary>
        /// Download image asynchronously
        /// </summary>
        /// <param name="fileUri"></param>
        /// <param name="fileName"></param>
        /// <param name="ct">ct</param>
        private static async Task<Tuple<string,Exception>> DownloadFile(string fileName, Uri fileUri, Constants.FileType ti, CancellationToken ct)
        {
            string PathDirectory = String.Empty;
            string extension = String.Empty;
            switch (ti)
            {
                case Constants.FileType.BackgroundImage:
                    PathDirectory = Constants.BackgroundMovieDirectory;
                    extension = ".jpg";
                    break;
                case Constants.FileType.CoverImage:
                    PathDirectory = Constants.CoverMoviesDirectory;
                    extension = ".jpg";
                    break;
                case Constants.FileType.PosterImage:
                    PathDirectory = Constants.PosterMovieDirectory;
                    extension = ".jpg";
                    break;
                case Constants.FileType.DirectorImage:
                    PathDirectory = Constants.DirectorMovieDirectory;
                    extension = ".jpg";
                    break;
                case Constants.FileType.ActorImage:
                    PathDirectory = Constants.ActorMovieDirectory;
                    extension = ".jpg";
                    break;
                case Constants.FileType.TorrentFile:
                    PathDirectory = Constants.TorrentDirectory;
                    extension = ".torrent";
                    break;
                default:
                    return new Tuple<string, Exception>(fileName, new Exception());
            }
            string downloadToDirectory = PathDirectory + fileName + extension;


            if (!Directory.Exists(PathDirectory))
            {
                try
                {
                    Directory.CreateDirectory(PathDirectory);
                }
                catch (Exception e)
                {
                    return new Tuple<string, Exception>(fileName, e);
                }
            }

            if (!File.Exists(downloadToDirectory))
            {
                try
                {
                    using (var webClient = new NoKeepAliveWebClient())
                    {
                        ct.Register(webClient.CancelAsync);
                        if (!File.Exists(downloadToDirectory))
                        {
                            try
                            {
                                await webClient.DownloadFileTaskAsync(fileUri,
                                    @downloadToDirectory);

                                try
                                {
                                    FileInfo fi = new FileInfo(downloadToDirectory);
                                    if (fi.Length == 0)
                                    {
                                        return new Tuple<string, Exception>(fileName, new Exception());
                                    }
                                }
                                catch (Exception e)
                                {
                                    return new Tuple<string, Exception>(fileName, e);
                                }

                            }
                            catch (Exception e)
                            {
                                return new Tuple<string, Exception>(fileName, e);
                            }
                        }
                        else
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(downloadToDirectory);
                                if (fi.Length == 0)
                                {
                                    try
                                    {
                                        File.Delete(downloadToDirectory);
                                        try
                                        {
                                            await webClient.DownloadFileTaskAsync(fileUri, @downloadToDirectory);

                                            FileInfo newfi = new FileInfo(downloadToDirectory);
                                            if (fi.Length == 0)
                                            {
                                                return new Tuple<string, Exception>(fileName, new Exception());
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            return new Tuple<string, Exception>(fileName, e);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        return new Tuple<string, Exception>(fileName, e);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                return new Tuple<string, Exception>(fileName, e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    return new Tuple<string, Exception>(fileName, e);
                }
            }

            return new Tuple<string, Exception>(fileName, null);
        }
        #endregion
        #endregion
    }    
}