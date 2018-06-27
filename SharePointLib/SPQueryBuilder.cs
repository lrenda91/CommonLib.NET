using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.SharePoint.Client;

namespace org.commitworld.web.business.sharepoint
{
    /// <summary>
    /// Classe che modella un nodo (generico) di una CAML Query, strutturato come binary tree a puntatori espliciti.
    /// Questi nodi rappresentano le condizioni logiche, i cui operandi saranno i nodi figli.
    /// Nota: il motivo per la struttura a soli 2 figli è dovuta ad un vincolo descritto nelle specifiche di CAML Query.
    /// Ogni nodo And, Or... ammette soli 2 nodi figli. Se la condizione logica prevede 3 o più operandi, 
    /// bisogna popolare nodi figli ricorsivamente.
    /// </summary>
    public class Node
    {
        public Node(string s)
        {
            xName = s;
        }
        private string xName;
        public Node[] children = new Node[2];

        public int ChildrenCount()
        {
            return children.Count((n) => n != null);
        }
        public bool isLeaf()
        {
            return ChildrenCount() == 0;
        }
        public override string ToString()
        {
            return get().ToString();
        }
        /// <summary>
        /// Costruisce il tag XML dell'intera CAML Query a partire dal nodo radice.
        /// Popola i nodi figli discendendo ricorsivamente 
        /// </summary>
        /// <returns>Il nodo XML completo, fino ai nodi foglia</returns>
        protected virtual XElement get()
        {
            XElement root = new XElement(xName);
            if (children[0] != null) root.Add(children[0].get());
            if (children[1] != null) root.Add(children[1].get());
            return root;
        }
    }

    /// <summary>
    /// Classe che modella un nodo FOGLIA di una CAML Query
    /// Questo nodo ospiterà le reali condizioni di ricerca
    /// </summary>
    public class LeafNode : Node
    {
        public XElement root { get; protected set; }
        public LeafNode(XElement elem)
            : base("Leaf")
        {
            root = elem;
        }
        protected override XElement get()
        {
            return root;
        }
    }

    /// <summary>
    /// Classe di utilità per costruire le CAML Queries di ricerca su SharePoint
    /// </summary>
    public class SPQueryBuilder
    {

        private Node where = new Node("Where");

        public string Build()
        {
            Node view = new Node("View");
            view.children[1] = new Node("Query");
            view.children[1].children[1] = where;
            return view.ToString();
        }

        /// <summary>
        /// Aggiunge una condizione (un nodo foglia) alla query.
        /// </summary>
        /// <param name="and">True per condizioni AND, false per condizini OR</param>
        /// <param name="conditions">L'insieme dei nodi XML foglia (delle condizioni semplici) da aggiungere</param>
        /// <returns></returns>
        private Node And(bool and, params XElement[] conditions)
        {
            if (conditions.Length < 2) return new LeafNode(conditions[0]);
            Node result = new Node(and ? "And" : "Or");
            Node father = result;
            foreach (XElement el in conditions)
            {
                if (father.isLeaf()) father.children[0] = new LeafNode(el);
                else if (father.ChildrenCount() == 1) father.children[1] = new LeafNode(el);
                else
                {
                    Node leaf = father.children.Last();
                    Node newNode = new Node(and ? "And" : "Or");
                    newNode.children[0] = leaf;
                    newNode.children[1] = new LeafNode(el);
                    father.children[1] = newNode;
                    father = newNode;
                }
            }
            return result;
        }

        /// <summary>
        /// Espone la funzionalità di aggiunta alla CAML query di una condizione di match per il valore di un Field sull'archivo documentale SharePoint 
        /// </summary>
        /// <param name="f">Il Field su cui fare comparazione</param>
        /// <param name="val">Il valore di match</param>
        public void AndFieldEqualTo(Field f, object val)
        {
            XElement fieldDef = XElement.Parse(f.SchemaXml);
            XElement fieldRefElement = new XElement("FieldRef", fieldDef.Attribute("Name"));
            XElement valueElement = new XElement("Value", fieldDef.Attribute("Type"), val);

            XElement eq = new XElement("Eq", fieldRefElement, valueElement);
            AddCondition(And(true, eq));
        }
        /// <summary>
        /// Espone la funzionalità di aggiunta alla CAML query di una condizione di match per il valore di un Field (di nome e tipo specificato)
        /// sull'archivo documentale SharePoint 
        /// </summary>
        /// <param name="fieldName">Il nome del Field</param>
        /// <param name="type">Il tipo del Field</param>
        /// <param name="val">Il valore di match</param>
        public void AndFieldEqualTo(string fieldName, FieldType type, object val)
        {
            XElement fieldRefElement = new XElement("FieldRef", new XAttribute("Name", fieldName));
            XElement valueElement = new XElement("Value", new XAttribute("Type", type.ToString()), val);

            XElement eq = new XElement("Eq", fieldRefElement, valueElement);
            AddCondition(And(true, eq));
        }

        /// <summary>
        /// Espone la funzionalità di aggiunta alla CAML query di una condizione di match tra i valori ammissibili di un Field con un determinato dominio.
        /// Utilizzato per cercare i match con campi di tipo MultiChoice (lista limitata di valori)
        /// </summary>
        /// <param name="f">Il Field su cui fare comparazione</param>
        /// <param name="values">La collection di valori da ricercare</param>
        /// <param name="containsAll">True se il Field deve contenere TUTTI i valori ricercati, FALSE altrimenti</param>
        public void AndFieldContains(Field f, ICollection values, bool containsAll)
        {
            XElement fieldDef = XElement.Parse(f.SchemaXml);
            List<XElement> elems = new List<XElement>();
            foreach (object val in values)
            {
                XElement fieldRefElement = new XElement("FieldRef", fieldDef.Attribute("Name"));
                XElement valueElement = new XElement("Value", fieldDef.Attribute("Type"), val);
                XElement contains = new XElement("Contains", fieldRefElement, valueElement);
                elems.Add(contains);
            }
            AddCondition(And(containsAll, elems.ToArray()));
        }

        /// <summary>
        /// Espone la funzionalità di ricerca di un file per nome.
        /// </summary>
        /// <param name="fileName">Il nome del file</param>
        /// <returns>La CAML Query completa come stringa</returns>
        public string GetFileQuery(string fileName)
        {
            XElement fieldRefElement = new XElement("FieldRef", new XAttribute("Name", "FileLeafRef"));
            XElement valueElement = new XElement("Value", new XAttribute("Type", FieldType.File.ToString()), fileName);
            XElement eq = new XElement("Eq", fieldRefElement, valueElement);

            AddCondition(And(true, eq));
            return Build();
        }

        /// <summary>
        /// Aggiunge il nodo specificato e gestisce eventuali riaggiustamenti della struttura dati per 3 o più operandi.
        /// </summary>
        /// <param name="node"></param>
        private void AddCondition(Node node)
        {
            Node father = getCandidate(where);
            if (father == where && where.isLeaf())
            {
                father.children[1] = node;
                return;
            }

            Node leaf = father.children.Last();
            Node f = new Node("And");
            f.children[0] = leaf;
            f.children[1] = node;

            father.children[1] = f;
        }

        /// <summary>
        /// Ricerca nel binary tree il nodo candidato ad essere popolato con la prossima condizione semplice.
        /// Scende ricorsivamente a partire dal nodo passato per parametro 
        /// </summary>
        /// <param name="node">Il nodo padre da cui iniziare la ricerca</param>
        /// <returns>Il nodo candidato ad ospitare la prossima condizione semplice</returns>
        private Node getCandidate(Node node)
        {
            int children = node.ChildrenCount();
            if (children == 0) return node;
            if (node.children.Last() != null && node.children.Last().isLeaf()) return node;
            return getCandidate(node.children.Last());
        }

    }
}
