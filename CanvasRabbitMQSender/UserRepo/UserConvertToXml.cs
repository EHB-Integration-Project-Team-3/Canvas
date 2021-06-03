using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace CanvasRabbitMQReceiver.UserRepo
{
    class UserConvertToXml
    {
        public static string convertToXml(User user)
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                
                XmlWriterSettings writersettings = new XmlWriterSettings();
                writersettings.Indent = true;
                writersettings.Encoding = Encoding.UTF8;
                using (var writer = XmlWriter.Create(stringWriter, writersettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("user");
                    //writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", null, "user.xsd");
                    writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
                    writer.WriteStartElement("header");
                    if (user.CreatedAt == user.UpdatedAt)
                    {
                        writer.WriteElementString("method", "CREATE");
                    }
                    else
                    {
                        if (user.Deleted)
                        {
                            writer.WriteElementString("method", "DELETE");
                        }
                        else
                        {
                            writer.WriteElementString("method", "UPDATE");
                        }
                    }
                    writer.WriteElementString("source", "CANVAS");
                    writer.WriteEndElement();
                    writer.WriteElementString("uuid", user.UUID);

                    writer.WriteElementString("entityVersion", "12");
                    string firstname = user.Sortable_name.Substring(0, user.Sortable_name.IndexOf(",") + 1).Replace(",", "");
                    string lastname = user.Sortable_name.Substring(user.Sortable_name.IndexOf(",") + 1).Replace(" ", "");
                    writer.WriteElementString("lastName", lastname);
                    writer.WriteElementString("firstName", firstname);
                    string email = user.Email.Replace(" ", ".");
                    writer.WriteElementString("emailAddress", email);
                    writer.WriteElementString("role", user.Role);
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                    writer.Flush();
                    writer.Close();
                }

                return stringWriter.ToString();
            }
        }
    }
}
