using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint.Client;

namespace org.commitworld.web.business.sharepoint
{
    /// <summary>
    /// Classe di accesso di più basso livello (Core) alle finzionalità dell'archivio documentale SharePoint.
    /// Fornisce le API di base per creare, cancellare e ricercare files, liste e cartelle.
    /// </summary>
    public class SharePointCore
    {
        private static string SharePointRoot;
        private static string SitePath;
        private static string AuthMode;
        private static string Username;
        private static string userPWD;
        private static string userDomain;

        /// <summary>
        /// Imposta i parametri di connessione all'archivio
        /// </summary>
        /// <param name="siteRoot">L'URL del site</param>
        /// <param name="sitePath">IL path relativo nel site</param>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="domain">Dominio di accesso</param>
        public static void SetUp(
            string siteRoot,
            string sitePath,
            string username,
            string password,
            string domain
        )
        {
            SharePointRoot = siteRoot;
            SitePath = sitePath;
            Username = username;
            userPWD = password;
            userDomain = domain;
        }

        /// <summary>
        /// Metodo di test di connessione con l'archivio documentale
        /// </summary>
        public static void test()
        {
            using (ClientContext context = CreateSiteContext())
            {
                Web web = context.Web;
                context.Load(web, (w) => w.ServerRelativeUrl);
                context.ExecuteQuery();
            }
        }

        /// <summary>
        /// Scarica in memoria una qualunque entità gestita da SharePoint
        /// </summary>
        /// <param name="obj">L'oggetto da richiedere</param>
        public static void Load(ClientObject obj)
        {
            using (ClientContext context = CreateSiteContext())
            {
                context.Load(obj);
                context.ExecuteQuery();
            }
        }

        /// <summary>
        /// API di creazione di una List
        /// </summary>
        /// <param name="listName">Il nome della nuova List</param>
        /// <param name="fieldDefinitionsXml">La lista dei path dei file XML di definizione dei campi</param>
        /// <returns>L'oggetto List di riferimento</returns>
        public static List CreateList(string listName, List<string> fieldDefinitionsXml)
        {
            using (ClientContext context = CreateSiteContext())
            {
                List list = null;
                ListCreationInformation libraryCreationInfo = new ListCreationInformation();
                libraryCreationInfo.Title = listName;
                libraryCreationInfo.TemplateType = (int)ListTemplateType.DocumentLibrary;
                list = context.Web.Lists.Add(libraryCreationInfo);

                if (fieldDefinitionsXml != null)
                {
                    foreach (string def in fieldDefinitionsXml)
                    {
                        list.Fields.AddFieldAsXml(def, true, AddFieldOptions.DefaultValue);
                    }
                }

                context.ExecuteQuery();
                return list;
            }
        }

        /// <summary>
        /// Scarica le informazioni su una determinata Folder.
        /// Nota: se la Folder non esiste, viene preventivamente creata 
        /// </summary>
        /// <param name="context">Il ClientContext di riferimento</param>
        /// <param name="listName">Il nome della List nella quale cercare oppure creare la Folder</param>
        /// <param name="folderName">Il nome della Folder da restituire. Se non passato, viene ricercata la RootFolder della List di riferimento</param>
        /// <returns>L'oggetto Folder di riferimento</returns>
        private static Folder GetOrCreateFolder(ClientContext context, string listName, string folderName)
        {
            List list = LoadList(context, listName);

            context.Load(list.RootFolder);
            context.Load(list.RootFolder.Folders);
            context.ExecuteQuery();

            if (folderName == null)
            {
                return list.RootFolder;
            }

            string folderUrl = String.Format("{0}/{1}/{2}", SitePath, listName, folderName);

            FolderCollection folders = list.RootFolder.Folders;
            var existingFolders = context.LoadQuery<Folder>(
                folders.Where(folder => folder.ServerRelativeUrl == folderUrl)
            );
            context.ExecuteQuery();

            Folder result = existingFolders.FirstOrDefault();

            if (result != null)
            {
                context.Load(result);
                context.Load(result.Files);
                context.ExecuteQuery();
                return result;
            }

            var itemCreateInfo = new ListItemCreationInformation
            {
                UnderlyingObjectType = FileSystemObjectType.Folder,
                LeafName = folderName
            };
            ListItem item = list.AddItem(itemCreateInfo);
            item["Title"] = folderName;
            item.Update();
            context.ExecuteQuery();

            existingFolders = context.LoadQuery<Folder>(
                folders.Where(folder => folder.ServerRelativeUrl == folderUrl)
            );
            context.ExecuteQuery();
            return existingFolders.First();
        }

        /// <summary>
        /// Scarica le informazioni su una determinata List, compresa collezione dei suoi campi
        /// </summary>
        /// <param name="listName">Il nome della List</param>
        /// <param name="listFields">La collezione dei campi della List</param>
        public static void GetList(string listName, out FieldCollection listFields)
        {
            using (ClientContext context = CreateSiteContext())
            {
                List list = LoadList(context, listName);
                listFields = LoadFields(context, list);
            }
        }

        /// <summary>
        /// API di Upload di un file sull'archivio documentale
        /// </summary>
        /// <param name="content">Il contenuto binario del file</param>
        /// <param name="fileName">Il nome col quale censire il nuovo file</param>
        /// <param name="listName">La List nella quale creare il file</param>
        /// <param name="folderName">La folder, all'interno della List, nella quale creare il file</param>
        /// <param name="metadata">I metadati associati al file, che saranno censiti come colonne aggiuntive nella Folder di destinazione</param>
        public static void UploadFile(byte[] content, string fileName, string listName, string folderName, Dictionary<string, object> metadata)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(listName))
            {
                throw new ArgumentException("File name and list name must not be null");
            }
            using (ClientContext context = CreateSiteContext())
            {
                FileCreationInformation fileCreationInfo = new FileCreationInformation();
                fileCreationInfo.Url = fileName;
                fileCreationInfo.Overwrite = true;
                fileCreationInfo.Content = content;

                Folder folder = GetOrCreateFolder(context, listName, folderName);
                File uploadedFile = folder.Files.Add(fileCreationInfo);
                if (metadata != null)
                {
                    foreach (KeyValuePair<string, object> pair in metadata)
                    {
                        uploadedFile.ListItemAllFields[pair.Key] = pair.Value;
                    }
                    uploadedFile.ListItemAllFields.Update();
                }
                context.ExecuteQuery();
            }
        }

        /// <summary>
        /// API di ricerca items nell'archivio
        /// </summary>
        /// <param name="listName">Il nome della List all'interno dell'archivio documentale in cui ricercare</param>
        /// <param name="folder">Il nome della folder all'interno della List</param>
        /// <param name="camlQueryXML">I parametri di ricerca come CAML Query</param>
        /// <returns></returns>
        public static ListItemCollection Find(string listName, string folder, string camlQueryXML)
        {
            using (ClientContext context = CreateSiteContext())
            {
                CamlQuery query = new CamlQuery();
                query.FolderServerRelativeUrl = GetOrCreateFolder(context, listName, folder).ServerRelativeUrl;
                query.ViewXml = camlQueryXML;

                List spList = LoadList(context, listName);
                ListItemCollection listItems = spList.GetItems(query);
                context.Load(listItems);
                context.ExecuteQuery();

                foreach (ListItem item in listItems)
                {
                    context.Load(item);
                    context.Load(item.File);
                    context.ExecuteQuery();
                }

                return listItems;
            }
        }

        /// <summary>
        /// Scarica il contenuto binario di un file dall'archivio documentale
        /// </summary>
        /// <param name="fileURL">L'url assocuta del file</param>
        /// <returns>Il contenuto binario del file come array di bytes</returns>
        public static byte[] DownloadFileContent(string fileURL)
        {
            using (ClientContext context = CreateSiteContext())
            {
                if (context.HasPendingRequest)
                {
                    context.ExecuteQuery();
                }
                FileInformation info = File.OpenBinaryDirect(context, fileURL);

                using (System.IO.Stream stream = info.Stream)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// API di cancellazione di un item dall'archivio, cercandolo in una determinata folder in una determinata List, e personalizzando la query con una CAML Query custom
        /// </summary>
        /// <param name="fileName">Il nome del file</param>
        /// <param name="listName">IL nome della List</param>
        /// <param name="camlQueryXML">I parametri di ricerca come CAML Query</param>
        /// <returns>true se il file era presente prima della cancellazione, false altrimenti</returns>
        public static bool Delete(string fileName, string listName, string camlQueryXML)
        {
            using (ClientContext context = CreateSiteContext())
            {
                List list = LoadList(context, listName);
                CamlQuery camlQuery = new CamlQuery();
                camlQuery.ViewXml = camlQueryXML;

                ListItemCollection listItems = list.GetItems(camlQuery);
                context.Load(listItems);
                context.ExecuteQuery();
                if (listItems.Count == 0)
                {
                    return false;
                }
                foreach (ListItem listitem in listItems)
                {
                    listitem.DeleteObject();
                    context.ExecuteQuery();
                }
            }
            return true;
        }

        /// <summary>
        /// Crea un ClientContext di riferimento per interagire con il site.
        /// Nota: Utilizzare lo stesso oggetto ClientContext restituito da questo metodo per TUTTE le interazioni 'atomiche' in una macro-operazione.
        /// </summary>
        /// <returns></returns>
        private static ClientContext CreateSiteContext()
        {
            string siteURL = string.Concat(SharePointRoot, SitePath);
            ClientContext context = new ClientContext(siteURL);
            //System.Net.NetworkCredential cred = new System.Net.NetworkCredential(Username, userPWD, userDomain);
            //context.Credentials = cred;
            return context;
        }

        private static List LoadList(ClientContext context, string listName)
        {
            List list = context.Web.Lists.GetByTitle(listName);
            list.EnableFolderCreation = true;
            context.Load(list);
            try
            {
                context.ExecuteQuery();
            }
            catch (ServerException se)
            {
                string notExistentListMsg = string.Format("'{0}' does not exist", listName);
                if (se.Message != null && se.Message.Contains(notExistentListMsg))
                {
                    throw new ArgumentException(notExistentListMsg);
                }
                throw se;
            }
            return list;
        }

        /// <summary>
        /// Metodo di utilità ad uso interno.
        /// Scarica la lista dei campi associati ad una determinata List.
        /// </summary>
        /// <param name="context">Il context di riferimento</param>
        /// <param name="list">La List del site dalla quale scaricare i dati associati ai campi</param>
        /// <returns>La FieldCollection da scorrere come Enumerable</returns>
        private static FieldCollection LoadFields(ClientContext context, List list)
        {
            FieldCollection fields = list.Fields;
            context.Load(fields);
            context.ExecuteQuery();
            return fields;
        }

    }
}
