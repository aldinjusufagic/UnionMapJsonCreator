using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Devices;
using System.Diagnostics;
using System.Drawing.Text;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace UnionMapCreator
{
    public partial class Form1 : Form
    {
        List<Continent> continents = new List<Continent>();
        List<Node> nodes = new List<Node>();

        public Image image;
        private float _zoomFactor = 1f;
        private const float ZoomStep = 0.1f;
        private float _initialScale;
        private int _mouseX = 0;
        private int _mouseY = 0;
        private int _imageOffsetX = 0;
        private int _imageOffsetY = 0;

        string currentNodeName = string.Empty;
        public Form1()
        {
            InitializeComponent();
            _zoomFactor = 1.0f; // Start with no zoom
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            image = Image.FromFile(@"UnionMap.png");

            pictureBox1.Paint += PictureBox1_Paint;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
        }
        private void PictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (image == null)
                return;

            // Calculate the aspect ratios
            float imageAspect = (float)image.Width / image.Height;
            float boxAspect = (float)pictureBox1.ClientSize.Width / pictureBox1.ClientSize.Height;

            // Determine the initial scaling factor to fit the image in the PictureBox
            if (imageAspect > boxAspect)
            {
                _initialScale = (float)pictureBox1.ClientSize.Width / image.Width;
            }
            else
            {
                _initialScale = (float)pictureBox1.ClientSize.Height / image.Height;
            }

            // Apply zoom factor to the initial scale
            float scaleFactor = _initialScale * _zoomFactor;

            // Calculate the new dimensions of the image
            int newWidth = (int)(image.Width * scaleFactor);
            int newHeight = (int)(image.Height * scaleFactor);

            // Calculate the offset to center the image
            int offsetX = (pictureBox1.ClientSize.Width - newWidth) / 2;
            int offsetY = (pictureBox1.ClientSize.Height - newHeight) / 2;

            // Draw the image with the calculated dimensions and offset
            e.Graphics.DrawImage(image, offsetX + _imageOffsetX, offsetY + _imageOffsetY, newWidth, newHeight);
        }
        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            // Save the previous zoom factor
            float previousZoomFactor = _zoomFactor;

            // Update the zoom factor based on mouse wheel scroll
            if (e.Delta > 0)
                _zoomFactor += ZoomStep; // Zoom in
            else if (e.Delta < 0)
                _zoomFactor = Math.Max(0.1f, _zoomFactor - ZoomStep); // Zoom out

            // Calculate the change in zoom factor
            float zoomChange = _zoomFactor / previousZoomFactor;

            // Adjust the image offset to keep zoom centered on the mouse position
            int mouseX = e.X;
            int mouseY = e.Y;

            // Convert mouse coordinates to image coordinates
            float imageX = (mouseX - pictureBox1.ClientSize.Width / 2) / _initialScale;
            float imageY = (mouseY - pictureBox1.ClientSize.Height / 2) / _initialScale;

            // Calculate the new offset
            _imageOffsetX -= (int)((imageX - (_imageOffsetX / _zoomFactor)) * (zoomChange - 1));
            _imageOffsetY -= (int)((imageY - (_imageOffsetY / _zoomFactor)) * (zoomChange - 1));

            // Trigger redraw
            pictureBox1.Invalidate();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // Ensure that the PictureBox handles the MouseWheel event
            pictureBox1.Focus();
            base.OnMouseWheel(e);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            pictureBox1.Invalidate(); // Redraw on resize
        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (NodeNameBox.Text != string.Empty)
            {
                if (nodes.Count == 0)
                {
                    addNode(e);
                }
                else
                {
                    if (!alreadyExists(NodeNameBox.Text))
                    {
                        addNode(e);
                    }
                }
            }
            else
                changeHintLabel("Please Name The Node Before Placing");
        }
        private void addNode(MouseEventArgs e)
        {
            Point point = e.Location;
            Point offset = new Point(point.X - 5, point.Y - 5);

            PictureBox pb = new PictureBox();
            pb.Location = offset;
            pb.Size = new Size(10, 10);
            pb.MouseDown += new MouseEventHandler(node_Click);
            pictureBox1.Controls.Add(pb);

            nodes.Add(new Node(NodeNameBox.Text, pb));
            Debug.WriteLine($"added Node: {nodes.Count}");
        }
        private void node_Click(object sender, MouseEventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            Node currentNode = getCurrentNode();

            Node clickedNode = getClickedNode(pictureBox);


            if (string.IsNullOrEmpty(currentNodeName))
            {
                currentNodeName = clickedNode.name;
                CurrentNodeLabel.Text = "Current Node: " + clickedNode.name;
                UpdateListBox();
                changeHintLabel("Select Another Node To Connect");
            }
            else if (hasCurrentNode())
            {
                if (currentNode == clickedNode)
                {
                    changeHintLabel("Cannot Add Current Node To Current Nodes Adjecent Nodes");
                }
                else
                {
                    if (!ClickedNodeIsConnected(clickedNode))
                    {
                        currentNode.adjecentList.Add(clickedNode.name);
                        UpdateListBox();
                        changeHintLabel("Nodes Connected Succesfully");
                    }
                }
            }
        }
        private bool ClickedNodeIsConnected(Node clickedNode)
        {
            Node currentNode = getCurrentNode();
            for (int i = 0; i < currentNode.adjecentList.Count; i++)
            {
                if (clickedNode.name == currentNode.adjecentList[i])
                {
                    changeHintLabel($"Selected Node {clickedNode.name} Is Already Connected To The Current Node");
                    return true;
                }
            }
            return false;
        }
        private Node getClickedNode(PictureBox pictureBox)
        {
            foreach (Node node in nodes)
            {
                if (node.pictureBox == pictureBox)
                {
                    return node;
                }
            }
            return null;
        }
        private void CurrentNodeLabel_TextChanged(object sender, EventArgs e)
        {
            UpdateListBox();
        }

        private void UpdateListBox()
        {
            BetterListItem itemControl;
            betterListBox2.clear();
            betterListBox3.clear();
            Node node = getCurrentNode();
            if (node != null)
            {
                for (int i = 0; i < node.adjecentList.Count; i++)
                {
                    itemControl = CreateListItem(node.adjecentList[i], ref betterListBox2);
                    itemControl.ButtonClick += NodeListItemClick;
                    betterListBox2.addItem(itemControl);
                }
                for (int j = 0; j < node.continentList.Count; j++)
                {
                    itemControl = CreateListItem(node.continentList[j], ref betterListBox3);
                    itemControl.ButtonClick += ConnectedContinentListItemClick;
                    betterListBox3.addItem(itemControl);
                }
            }
        }
        private void ContinentListItemClick(object sender, EventArgs e)
        {
            BetterListItem clickedItem = sender as BetterListItem;
            RemoveItemFromLists(clickedItem, betterListBox1);
        }
        private void NodeListItemClick(object sender, EventArgs e)
        {
            BetterListItem clickedItem = sender as BetterListItem;
            RemoveItemFromLists(clickedItem, betterListBox2);
        }
        private void ConnectedContinentListItemClick(object sender, EventArgs e)
        {
            BetterListItem clickedItem = sender as BetterListItem;
            RemoveItemFromLists(clickedItem, betterListBox3);
        }
        private void CancelButton_Click(object sender, EventArgs e)
        {
            deSelectCurrentNode();
        }
        private BetterListItem CreateListItem(string text, ref BetterListBox box)
        {
            BetterListItem itemControl = new BetterListItem
            {
                ItemText = text,
                Width = box.Width - 2,
                Location = new Point(0, box.items.Count * 24), //Adjust height as needed
            };

            return itemControl;
        }
        private void CreateButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ContinentNameBox.Text))
            {
                changeHintLabel("Please Enter a Name");
            }
            else if (string.IsNullOrEmpty(ContinentTroopBox.Text))
            {
                changeHintLabel("Please Enter Troop Bonus Amount");
            }
            else
            {
                if (int.TryParse(ContinentTroopBox.Text, out int result))
                {
                    continents.Add(new Continent(ContinentNameBox.Text, int.Parse(ContinentTroopBox.Text)));

                    BetterListItem itemControl = CreateListItem(ContinentNameBox.Text, ref betterListBox1);
                    itemControl.ButtonClick += ContinentListItemClick;
                    betterListBox1.addItem(itemControl);
                }
                else
                {
                    changeHintLabel("Troop Bonus Must Be Of Type Integer");
                }
            }
        }

        private void RemoveItemFromLists(BetterListItem clickedItem, BetterListBox box)
        {
            int index = box.items.IndexOf(clickedItem);
            box.removeItem(index);

            string boxName = box.Name;

            if (boxName == "betterListBox1")
            {
                for (int i = 0; i < continents.Count; i++)
                {
                    if (clickedItem.ItemText == continents[i].name)
                    {
                        RemoveContinentInOtherBox(continents[i]);
                        continents.Remove(continents[i]);
                    }
                }
            }
            if (boxName == "betterListBox2")
            {
                Node currentNode = getCurrentNode();
                for (int i = 0; i < currentNode.adjecentList.Count; i++)
                {
                    if (clickedItem.ItemText == currentNode.adjecentList[i])
                    {
                        changeHintLabel($"Connection Between {currentNode.name} And {currentNode.adjecentList[i]} Removed Succesfully");
                        currentNode.adjecentList.Remove(currentNode.adjecentList[i]);
                    }
                }
            }
            if (boxName == "betterListBox3")
            {
                Node currentNode = getCurrentNode();
                for (int i = 0; i < currentNode.continentList.Count; i++)
                {
                    changeHintLabel($"Removed {currentNode.continentList[i]} From {currentNode.name}");
                    currentNode.continentList.Remove(currentNode.continentList[i]);
                }
            }
        }
        private void RemoveContinentInOtherBox(Continent continent)
        {
            for (int i = 0; i < betterListBox3.items.Count; i++)
            {
                if (continent.name == betterListBox3.items[i].ItemText)
                {
                    BetterListItem item = betterListBox3.items[i];
                    int index = betterListBox3.items.IndexOf(item);
                    betterListBox3.removeItem(index);
                    RemoveItemFromLists(item, betterListBox3);
                }
            }
        }
        private void CreateFileButton_Click(object sender, EventArgs e)
        {
            foreach (Node node in nodes)
            {
                node.adjecentNames = node.adjecentList.ToArray();
                node.continentNames = node.continentList.ToArray();
            }

            Map map = new Map(continents, nodes);

            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string fileName = "map.json";
            string filePath = Path.Combine(currentDirectory, fileName);

            string jsonString = JsonSerializer.Serialize(map);
            Debug.WriteLine(jsonString);
            File.WriteAllText(filePath, jsonString);

            MessageBox.Show("File saved successfully at " + filePath);
        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {

            if (hasCurrentNode())
            {
                if (continents.Count != 0)
                {
                    comboBox1.Items.Clear();
                    foreach (Continent continent in continents)
                    {
                        comboBox1.Items.Add(continent.name);
                    }
                }
                else
                {
                    changeHintLabel("There Are No Existing Continents");
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (hasCurrentNode())
            {
                if (!string.IsNullOrEmpty(comboBox1.SelectedIndex.ToString()))
                {
                    Node node = getCurrentNode();
                    if (node.continentList.Count == 0)
                    {
                        node.continentList.Add(comboBox1.Text);
                        UpdateListBox();
                    }
                    else
                    {
                        for (int i = 0; i < node.continentList.Count; i++)
                        {
                            if (node.continentList[i] == comboBox1.Text)
                            {
                                changeHintLabel("Cannot Add The Same Continent Twice");
                            }
                            else
                            {
                                node.continentList.Add(comboBox1.Text);
                                UpdateListBox();
                            }
                        }
                    }
                }
            }
        }
        private void changeHintLabel(string message)
        {
            HintLabel.Text = "Hint: " + message;
        }
        private Node getCurrentNode()
        {
            if (hasCurrentNode())
            {
                foreach (Node node in nodes)
                {
                    if (currentNodeName == node.name)
                    {
                        return node;
                    }
                }
                throw new Exception("Current Node Does Not Exist");
            }
            else
            {
                return null;
            }
        }
        private bool alreadyExists(string text)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].name == text)
                {
                    changeHintLabel("Node Already Exists");
                    return true;
                }
            }
            return false;
        }
        private bool hasCurrentNode()
        {
            if (string.IsNullOrEmpty(currentNodeName))
            {
                changeHintLabel("Please Select a Node");
                return false;
            }
            else { return true; }
        }
        private void deSelectCurrentNode()
        {
            currentNodeName = string.Empty;
            CurrentNodeLabel.Text = string.Empty;
        }
    }
}