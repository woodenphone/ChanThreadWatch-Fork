using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Web;

namespace JDP {
	public class SiteHelper {
		protected string _url = String.Empty;
		protected HTMLParser _htmlParser;

		public static SiteHelper GetInstance(string host) {
			Type type = null;
			try {
				string ns = (typeof(SiteHelper)).Namespace;
				string[] hostSplit = host.ToLower(CultureInfo.InvariantCulture).Split('.');
				for (int i = 0; i < hostSplit.Length - 1; i++) {
					type = Assembly.GetExecutingAssembly().GetType(ns +	".SiteHelper_" +
						String.Join("_", hostSplit, i, hostSplit.Length - i));
					if (type != null) break;
				}
			}
			catch { }
			if (type == null) type = typeof(SiteHelper);
			return (SiteHelper)Activator.CreateInstance(type);
		}

		public void SetURL(string url) {
			_url = url;
		}

		public void SetHTMLParser(HTMLParser htmlParser) {
			_htmlParser = htmlParser;
		}

		protected string[] SplitURL() {
			int pos = _url.IndexOf("://", StringComparison.Ordinal);
			if (pos == -1) return new string[0];
			return _url.Substring(pos + 3).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
		}

		public virtual string GetSiteName() {
			string[] hostSplit = (new Uri(_url)).Host.Split('.');
			return (hostSplit.Length >= 2) ? hostSplit[hostSplit.Length - 2] : String.Empty;
		}

		public virtual string GetBoardName() {
			string[] urlSplit = SplitURL();
			return (urlSplit.Length >= 3) ? urlSplit[1] : String.Empty;
		}

		public virtual string GetThreadName() {
			string[] urlSplit = SplitURL();
			if (urlSplit.Length >= 3) {
				string page = urlSplit[urlSplit.Length - 1];
				int pos = page.IndexOf('?');
				if (pos != -1) page = page.Substring(0, pos);
				pos = page.LastIndexOf('.');
				if (pos != -1) page = page.Substring(0, pos);
				return page;
			}
			return String.Empty;
		}

		public virtual bool IsBoardHighTurnover() {
			return false;
		}

		protected virtual string ImageURLKeyword {
			get { return "/src/"; }
		}

		public virtual List<ImageInfo> GetImages(List<ReplaceInfo> replaceList, List<ThumbnailInfo> thumbnailList) {
			HashSet<string> imageFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string> thumbnailFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<ImageInfo> imageList = new List<ImageInfo>();
			HTMLAttribute attribute;
			string url;
			int pos;

			foreach (HTMLTag linkTag in _htmlParser.FindStartTags("a")) {
				attribute = linkTag.GetAttribute("href");
				if (attribute == null) continue;
				url = General.GetAbsoluteURL(_url, HttpUtility.HtmlDecode(attribute.Value));
				if (url == null || url.IndexOf(ImageURLKeyword, StringComparison.OrdinalIgnoreCase) == -1) continue;

				HTMLTag linkEndTag = _htmlParser.FindCorrespondingEndTag(linkTag);
				if (linkEndTag == null) continue;

				ImageInfo image = new ImageInfo();
				ThumbnailInfo thumb = null;

				image.URL = url;
				if (image.URL == null || image.FileName.Length == 0) continue;
				pos = Math.Max(
					image.URL.LastIndexOf("http://", StringComparison.OrdinalIgnoreCase),
					image.URL.LastIndexOf("https://", StringComparison.OrdinalIgnoreCase));
				if (pos == -1) {
					image.Referer = _url;
				}
				else {
					image.Referer = image.URL;
					image.URL = image.URL.Substring(pos);
				}
				if (replaceList != null) {
					replaceList.Add(
						new ReplaceInfo {
							Offset = attribute.Offset,
							Length = attribute.Length,
							Type = ReplaceType.ImageLinkHref,
							Tag = image.FileName
						});
				}

				HTMLTag imageTag = _htmlParser.FindStartTag(linkTag, linkEndTag, "img");
				if (imageTag != null) {
					attribute = imageTag.GetAttribute("src");
					if (attribute != null) {
						url = General.GetAbsoluteURL(_url, HttpUtility.HtmlDecode(attribute.Value));
						if (url != null) {
							thumb = new ThumbnailInfo();
							thumb.URL = url;
							thumb.Referer = _url;
							if (replaceList != null) {
								replaceList.Add(
									new ReplaceInfo {
										Offset = attribute.Offset,
										Length = attribute.Length,
										Type = ReplaceType.ImageSrc,
										Tag = thumb.FileName
									});
							}
						}
					}
				}

				if (!imageFileNames.Contains(image.FileName)) {
					imageList.Add(image);
					imageFileNames.Add(image.FileName);
				}
				if (thumb != null && !thumbnailFileNames.Contains(thumb.FileName)) {
					thumbnailList.Add(thumb);
					thumbnailFileNames.Add(thumb.FileName);
				}
			}

			return imageList;
		}

		public virtual string GetNextPageURL() {
			return null;
		}

        public virtual bool ForceOneTimeDownload()
        // Setting to prevent from rechecking automatically.
        {
            return false;
        }
	}

    
    public class SiteHelper_4chan_org : SiteHelper {
    // //Commented out to fix problem saving images
    //    public override List<ImageInfo> GetImages(List<ReplaceInfo> replaceList, List<ThumbnailInfo> thumbnailList) {
    //        List<ImageInfo> imageList = new List<ImageInfo>();
    //        bool seenSpoiler = false;

    //        foreach (HTMLTagRange postTagRange in Enumerable.Where(Enumerable.Select(Enumerable.Where(_htmlParser.FindStartTags("div"),
    //            t => HTMLParser.ClassAttributeValueHas(t, "post")), t => _htmlParser.CreateTagRange(t)), r => r != null))
    //        {
    //            HTMLTagRange fileTextSpanTagRange = _htmlParser.CreateTagRange(Enumerable.FirstOrDefault(Enumerable.Where(
    //                _htmlParser.FindStartTags(postTagRange, "span"), t => HTMLParser.ClassAttributeValueHas(t, "fileText"))));
    //            if (fileTextSpanTagRange == null) continue;

    //            HTMLTagRange fileThumbLinkTagRange = _htmlParser.CreateTagRange(Enumerable.FirstOrDefault(Enumerable.Where(
    //                _htmlParser.FindStartTags(postTagRange, "a"), t => HTMLParser.ClassAttributeValueHas(t, "fileThumb"))));
    //            if (fileThumbLinkTagRange == null) continue;

    //            HTMLTag fileTextLinkStartTag = _htmlParser.FindStartTag(fileTextSpanTagRange, "a");
    //            if (fileTextLinkStartTag == null) continue;

    //            HTMLTag fileThumbImageTag = _htmlParser.FindStartTag(fileThumbLinkTagRange, "img");
    //            if (fileThumbImageTag == null) continue;

    //            string imageURL = fileTextLinkStartTag.GetAttributeValue("href");
    //            if (imageURL == null) continue;

    //            string thumbURL = fileThumbImageTag.GetAttributeValue("src");
    //            if (thumbURL == null) continue;

    //            bool isSpoiler = HTMLParser.ClassAttributeValueHas(fileThumbLinkTagRange.StartTag, "imgspoiler");

    //            string originalFileName;
    //            if (isSpoiler) {
    //                originalFileName = fileTextSpanTagRange.StartTag.GetAttributeValue("title");
    //            }
    //            else {
    //                HTMLTag fileNameSpanStartTag = _htmlParser.FindStartTag(fileTextSpanTagRange, "span");
    //                if (fileNameSpanStartTag == null) continue;
    //                originalFileName = fileNameSpanStartTag.GetAttributeValue("title");
    //            }
    //            if (originalFileName == null) continue;

    //            string imageMD5 = fileThumbImageTag.GetAttributeValue("data-md5");
    //            if (imageMD5 == null) continue;

    //            ImageInfo image = new ImageInfo {
    //                URL = General.GetAbsoluteURL(_url, HttpUtility.HtmlDecode(imageURL)),
    //                Referer = _url,
    //                OriginalFileName = General.CleanFileName(HttpUtility.HtmlDecode(originalFileName)),
    //                HashType = HashType.MD5,
    //                Hash = General.TryBase64Decode(imageMD5)
    //            };
    //            if (image.URL.Length == 0 || image.FileName.Length == 0 || image.Hash == null) continue;

    //            ThumbnailInfo thumb = new ThumbnailInfo {
    //                URL = General.GetAbsoluteURL(_url, HttpUtility.HtmlDecode(thumbURL)),
    //                Referer = _url
    //            };
    //            if (thumb.URL == null || thumb.FileName.Length == 0) continue;

    //            if (replaceList != null) {
    //                HTMLAttribute attribute;

    //                attribute = fileTextLinkStartTag.GetAttribute("href");
    //                if (attribute != null) {
    //                    replaceList.Add(
    //                        new ReplaceInfo {
    //                            Offset = attribute.Offset,
    //                            Length = attribute.Length,
    //                            Type = ReplaceType.ImageLinkHref,
    //                            Tag = image.FileName
    //                        });
    //                }

    //                attribute = fileThumbLinkTagRange.StartTag.GetAttribute("href");
    //                if (attribute != null) {
    //                    replaceList.Add(
    //                        new ReplaceInfo {
    //                            Offset = attribute.Offset,
    //                            Length = attribute.Length,
    //                            Type = ReplaceType.ImageLinkHref,
    //                            Tag = image.FileName
    //                        });
    //                }

    //                attribute = fileThumbImageTag.GetAttribute("src");
    //                if (attribute != null) {
    //                    replaceList.Add(
    //                        new ReplaceInfo {
    //                            Offset = attribute.Offset,
    //                            Length = attribute.Length,
    //                            Type = ReplaceType.ImageSrc,
    //                            Tag = thumb.FileName
    //                        });
    //                }
    //            }

    //            imageList.Add(image);

    //            if (!isSpoiler || !seenSpoiler) {
    //                thumbnailList.Add(thumb);
    //                if (isSpoiler) seenSpoiler = true;
    //            }
    //        }

    //        return imageList;
    //    }

        public override bool IsBoardHighTurnover() {
            return String.Equals(GetBoardName(), "b", StringComparison.OrdinalIgnoreCase);
        }


        // Override for new 4chan namespaces
        protected override string ImageURLKeyword
        {
            get { return "i.4cdn.org"; }
        }


        // Override to convert new thread url format into something that will give the old style output
        public override string GetThreadName()
        {
            
            
            string[] urlSplit = SplitURL();
            if (_url.ToLower().Contains("/thread/".ToLower()))
            {
                // New style thread namespace
                // “http(s)://boards.4chan.org/g/thread/41321419/daily-programming-thread”
                if (urlSplit.Length >= 3)
                {
                    string page = urlSplit[3];
                    int pos = page.IndexOf('?');
                    if (pos != -1) page = page.Substring(0, pos);
                    pos = page.LastIndexOf('.');
                    if (pos != -1) page = page.Substring(0, pos);
                    return page;
                }
                return String.Empty;
            }
            else
            {
                // For old style links
                // “http(s)://boards.4chan.org/g/res/41321419”
                if (urlSplit.Length >= 3)
                {
                    string page = urlSplit[urlSplit.Length - 1];
                    int pos = page.IndexOf('?');
                    if (pos != -1) page = page.Substring(0, pos);
                    pos = page.LastIndexOf('.');
                    if (pos != -1) page = page.Substring(0, pos);
                    return page;
                }
                return String.Empty;
            }
        }


    }


	public class SiteHelper_krautchan_net : SiteHelper {
		public override string GetThreadName() {
			string threadName = base.GetThreadName();
			if (threadName.StartsWith("thread-", StringComparison.OrdinalIgnoreCase)) {
				threadName = threadName.Substring(7);
			}
			return threadName;
		}

		protected override string ImageURLKeyword {
			get { return "/files/"; }
		}
	}
    public class SiteHelper_archive_heinessen_com : SiteHelper
    // For the http://archive.heinessen.com/ archive.
    // TODO: limit to one file download at a time.
    {
        protected override string ImageURLKeyword
        // Get images to work for this archive
        // Example image URL
        // <a href="/boards/mlp/img/0187/97/1405734311702.png">  <img class="thumb" src="/boards/mlp/thumb/0187/97/1405734311702s.jpg" alt="18797271" width="125" height="70"> </a>
        {
            get { return "/img/"; }
        }

        public override bool ForceOneTimeDownload()
        // Prevent from rechecking automatically.
        {
            return true;
        }
    }

    public class SiteHelper_archive_foolz_us : SiteHelper
    // For the http://archive.foolz.us/ archive.
    // TODO: limit to one file download at a time.
    {
        protected override string ImageURLKeyword
        // Get images to work for this archive
        // Example image URL
        // <a href="http://1-media-cdn.foolz.us/ffuuka/board/tg/image/1400/99/1400992506959.jpg" target="_blank" rel="noreferrer" class="thread_image_link"> <img src="http://1-media-cdn.foolz.us/ffuuka/board/tg/thumb/1400/99/1400992506959s.jpg" width="125" height="88" class="lazyload post_image" data-md5="GyWwXlbqohSt6KmLtGb8uw=="> </a>
        {
            get { return "/image/"; }
        }

        public override bool ForceOneTimeDownload()
        // Prevent from rechecking automatically.
        {
            return true;
        }
    }

    public class SiteHelper_archive_4plebs_org : SiteHelper
    // For the http://archive.4plebs.org/ archive.
    // TODO: limit to one file download at a time.
    {
        protected override string ImageURLKeyword
        // Get images to work for this archive
        // Example image URL
        // <a href="http://img.4plebs.org/boards/tg/image/1405/73/1405731101946.jpg" target="_blank" rel="noreferrer" class="thread_image_link"> <img src="http://img.4plebs.org/boards/tg/thumb/1405/73/1405731101946s.jpg" width="125" height="62" class="lazyload post_image" data-md5="23m6xQqIO1aTdoENnURyXQ=="> </a>
        {
            get { return "/image/"; }
        }

        public override bool ForceOneTimeDownload()
        // Prevent from rechecking automatically.
        {
            return true;
        }
    }
}
