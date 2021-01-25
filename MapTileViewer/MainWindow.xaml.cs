using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using System.Windows.Media.Media3D;

using MapMess;

namespace MapTileViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CameraController cameraController;

        Dictionary<string, GeometryModel3D> loadedTiles = new Dictionary<string, GeometryModel3D> { };

        Model3DGroup modelGroup = new Model3DGroup();
        AmbientLight ambientLight = new AmbientLight();

        Material whiteMaterial;

        public MainWindow()
        {
            InitializeComponent();

            cameraController = new CameraController(MainCamera);
            
            ambientLight.Color = Color.FromRgb(255, 255, 255);
            modelGroup.Children.Add(ambientLight);

            whiteMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(255, 255, 255)));

            //mesh.Positions.Add(new Point3D(-0.5, -0.5, 0.5));

            //mesh.TriangleIndices.Add(0);

            //mesh.Normals.Add(new Vector3D(0, 0, -1));

            //mesh.TextureCoordinates.Add(new Point(1, 0));

            //MapDownloader.GetConfig();

            //Brush brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            //RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            //AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            //myAxisAngleRotation3d.Axis = new Vector3D(0, 3, 0);
            //myAxisAngleRotation3d.Angle = 40;
            //myRotateTransform3D.Rotation = myAxisAngleRotation3d;
            //geometryModel.Transform = myRotateTransform3D;


        }

        #region Movement
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePos = e.GetPosition(this);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }

        Point lastMousePos;
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            InfoLabel.Content = 
                "center: " + cameraController.center.ToString() +
                "\ncamera: " + cameraController.camera.Position.ToString();
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point delta = lastMousePos - (Vector)e.GetPosition(this);
                lastMousePos = e.GetPosition(this);
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    cameraController.Move(new Vector3D(-delta.X / this.ActualWidth * 0.5, -delta.Y / this.ActualHeight * 0.5, 0));
                }
                else
                {
                    cameraController.Rotate(delta.X / this.ActualWidth * 360, delta.Y / this.ActualHeight * 360);
                }
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            cameraController.Zoom((double)e.Delta / 500);
        }
        #endregion

        #region Expander Sizing
        const int expanderHeight = 200;
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {

            var expander = (Expander)sender;
            Grid grid = expander.Content as Grid;
            expander.Height = 23 + expanderHeight;

            UpdateExpanderSpacing(expander.Parent as Grid, expander, expanderHeight);
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            var expander = (Expander)sender;
            expander.Height = 23;

            UpdateExpanderSpacing(expander.Parent as Grid, expander, -expanderHeight);
        }

        void UpdateExpanderSpacing(Grid grid, Expander activeExpander, int amount)
        {
            foreach (var element in grid.Children)
            {
                if (element.GetType() == typeof(Expander))
                {
                    var expElement = (Expander)element;
                    if (expElement.Margin.Top > activeExpander.Margin.Top)
                        expElement.Margin = new Thickness(expElement.Margin.Left, expElement.Margin.Top + amount, expElement.Margin.Right, expElement.Margin.Bottom);
                }
            }
        }
        #endregion

        #region Loading
        void LoadMapTile(string path)
        {
            GeometryModel3D geometryModel = new GeometryModel3D();
            MeshGeometry3D mesh = new MeshGeometry3D();
            ModelVisual3D modelVisual = new ModelVisual3D();

            BinaryReader binaryReader = new BinaryReader(File.Open(path, FileMode.Open));
            //MapDownloader.GetMap(21,570398,354669)
            MapReader mapReader = new MapReader(binaryReader);
            mapReader.ReadBinary();
            mapReader.ReadHeader();
            MapMesh mapMesh = mapReader.ReadMesh();

            binaryReader.Close();

            int x = 0;
            int y = 0;
            int z = 0;

            for (int i = 0; i < mapMesh.vertices.Length - 3; i += 3, x = mapMesh.vertices[i], y = mapMesh.vertices[i + 1], z = mapMesh.vertices[i + 2])
            {
                mesh.Positions.Add(new Point3D((double)x / 65536, (double)y / 65536, (double)z / 65536));
            }

            for (int i = 3; i < mapMesh.indices.Length; i++)
            {
                mesh.TriangleIndices.Add(mapMesh.indices[i]);
            }

            for (int i = 0; i < mapMesh.internalUVs.Length - 2; i += 2, x = mapMesh.internalUVs[i], y = mapMesh.internalUVs[i + 1])
            {
                mesh.TextureCoordinates.Add(new Point((double)x / 65536, (double)y / 65536));
            }

            //for (int i = 0; i < Mapper.n.indices.Length; i++)
            //{
            //    x = Mapper.n.vertices[Mapper.n.indices[i]*3];
            //    y = Mapper.n.vertices[Mapper.n.indices[i]*3+1];
            //    z = Mapper.n.vertices[Mapper.n.indices[i]*3+2];
            //    int uvx = Mapper.n.internalUVs[Mapper.n.indices[i]*2];
            //    int uvy = Mapper.n.internalUVs[Mapper.n.indices[i]*2+1];
            //    mesh.Positions.Add(new Point3D((double)x / 65536, (double)y / 65536, (double)z / 65536));
            //    mesh.TextureCoordinates.Add(new Point((double)uvx / 65536, (double)uvy / 65536));
            //}

            double maxSize = Math.Max(mapMesh.bboxMax[0] - mapMesh.bboxMin[0], Math.Max(mapMesh.bboxMax[1] - mapMesh.bboxMin[1], mapMesh.bboxMax[2] - mapMesh.bboxMin[2]));
            double xs = mapMesh.bboxMax[0] - mapMesh.bboxMin[0];
            double ys = mapMesh.bboxMax[1] - mapMesh.bboxMin[1];
            double zs = mapMesh.bboxMax[2] - mapMesh.bboxMin[2];
            Vector3D fitScale = new Vector3D((xs / maxSize), (ys / maxSize), (zs / maxSize));

            geometryModel.Material = whiteMaterial;
            geometryModel.BackMaterial = whiteMaterial;

            Transform3DGroup transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new TranslateTransform3D());
            Quaternion quat = new Quaternion(new Vector3D(1, 0, 0), 15) * new Quaternion(new Vector3D(0, 1, 0), -40);
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(quat.Axis, quat.Angle)));
            transformGroup.Children.Add(new ScaleTransform3D(fitScale));
            geometryModel.Transform = transformGroup;

            geometryModel.Geometry = mesh;
            modelGroup.Children.Add(geometryModel);
            modelVisual.Content = modelGroup;
            Viewport.Children.Add(modelVisual);

            string tileName = System.IO.Path.GetFileName(path);
            if (loadedTiles.ContainsKey(tileName))
            {
                int num = 1;
                while(loadedTiles.ContainsKey(tileName + num))
                {
                    num++;
                }
                loadedTiles.Add(tileName + num, geometryModel);
            }
            else
                loadedTiles.Add(tileName, geometryModel);
        }

        void LoadTexture(string path, GeometryModel3D geometryModel)
        {
            ImageBrush brush = new ImageBrush();
            brush.ImageSource = new BitmapImage(new Uri(path, UriKind.Absolute));
            DiffuseMaterial material = new DiffuseMaterial(brush);

            geometryModel.Material = material;
            geometryModel.BackMaterial = material;
        }
        #endregion

        void RefreshTabs()
        {
            UpdateTransformTab();
            RefreshTextureutton_Click(null, new RoutedEventArgs());
        }

        #region Geometry Tab
        private void LoadGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Filter = "*.bin|*.bin|*.*|*.*";
            fileDialog.InitialDirectory = Environment.CurrentDirectory;
            if (fileDialog.ShowDialog() == true && File.Exists(fileDialog.FileName))
                LoadMapTile(fileDialog.FileName);
            else
                return;

            TileListBox.Items.Clear();
            foreach (string name in loadedTiles.Keys)
            {
                TileListBox.Items.Add(name);
            }

            TileListBox.SelectedItem = TileListBox.Items[TileListBox.Items.Count - 1];
            RefreshTabs();
        }

        private void RemoveGeometryButton_Click(object sender, RoutedEventArgs e)
        {
            if(loadedTiles.ContainsKey((string)TileListBox.SelectedItem))
            {
                modelGroup.Children.Remove(loadedTiles[(string)TileListBox.SelectedItem]);
                loadedTiles.Remove((string)TileListBox.SelectedItem);
            }

            TileListBox.Items.Clear();
            foreach (string name in loadedTiles.Keys)
            {
                TileListBox.Items.Add(name);
            }
        }

        private void TileListBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RefreshTabs();
        }
        #endregion

        #region Transform Tab
        void UpdateTransformTab()
        {
            if (TileListBox.SelectedValue == null)
                return;

            Matrix3D matrix = loadedTiles[(string)TileListBox.SelectedValue].Transform.Value;

            TransformPositionX.Text = matrix.OffsetX.ToString();
            TransformPositionY.Text = matrix.OffsetY.ToString();
            TransformPositionZ.Text = matrix.OffsetZ.ToString();

            System.Numerics.Matrix4x4 mat = new System.Numerics.Matrix4x4();
            mat.M11 = (float)matrix.M11;
            mat.M44 = (float)matrix.M44;
            mat.M34 = (float)matrix.M34;
            mat.M33 = (float)matrix.M33;
            mat.M31 = (float)matrix.M31;
            mat.M32 = (float)matrix.M32;
            mat.M23 = (float)matrix.M23;
            mat.M22 = (float)matrix.M22;
            mat.M21 = (float)matrix.M21;
            mat.M14 = (float)matrix.M14;
            mat.M13 = (float)matrix.M13;
            mat.M12 = (float)matrix.M12;
            mat.M24 = (float)matrix.M24;
            System.Numerics.Quaternion quat = System.Numerics.Quaternion.CreateFromRotationMatrix(mat);
            Quaternion quaternion = new Quaternion();
            quaternion.X = quat.X;
            quaternion.Y = quat.Y;
            quaternion.Z = quat.Z;
            quaternion.W = quat.W;

            Vector3D rotation = ToEuler(quaternion);

            TransformRotationX.Text = rotation.X.ToString();
            TransformRotationY.Text = rotation.Y.ToString();
            TransformRotationZ.Text = rotation.Z.ToString();

            TransformScaleX.Text = matrix.M11.ToString();
            TransformScaleY.Text = matrix.M22.ToString();
            TransformScaleZ.Text = matrix.M33.ToString();
        }

        // transform is broken right now
        private void Transform_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(TransformPositionX.Text, out double positionX) &&
                double.TryParse(TransformPositionY.Text, out double positionY) &&
                double.TryParse(TransformPositionZ.Text, out double positionZ) &&
                double.TryParse(TransformRotationX.Text, out double rotationX) &&
                double.TryParse(TransformRotationY.Text, out double rotationY) &&
                double.TryParse(TransformRotationZ.Text, out double rotationZ) &&
                double.TryParse(TransformScaleX.Text, out double scaleX) &&
                double.TryParse(TransformScaleY.Text, out double scaleY) &&
                double.TryParse(TransformScaleZ.Text, out double scaleZ))
            {
                //Transform3DGroup transformGroup = new Transform3DGroup();
                //System.Numerics.Quaternion quat = System.Numerics.Quaternion.CreateFromYawPitchRoll((float)rotationX, (float)rotationY, (float)rotationZ);
                //Quaternion quaternion = new Quaternion();
                //quaternion.X = quat.X;
                //quaternion.Y = quat.Y;
                //quaternion.Z = quat.Z;
                //quaternion.W = quat.W;
                //transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(quaternion.Axis, quaternion.Angle)));
                //transformGroup.Children.Add(new ScaleTransform3D(new Vector3D(scaleX, scaleY, scaleZ)));
                //transformGroup.Children.Add(new TranslateTransform3D(new Vector3D(positionX, positionY, positionZ)));
                if (TileListBox.SelectedItem != null)
                {
                    //loadedTiles[(string)TileListBox.SelectedItem].Transform = transformGroup;
                    Matrix3D matrix = loadedTiles[(string)TileListBox.SelectedItem].Transform.Value;
                    matrix.OffsetX = positionX;
                    matrix.OffsetY = positionY;
                    matrix.OffsetZ = positionZ;
                    matrix.M11 = scaleX;
                    matrix.M22 = scaleY;
                    matrix.M33 = scaleZ;
                    loadedTiles[(string)TileListBox.SelectedItem].Transform = new MatrixTransform3D(matrix);
                }
            }
            else
            {
                if (TileListBox.SelectedValue != null)
                    MessageBox.Show("Unreadable input!", "Unreadable input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            UpdateTransformTab();
        }

        public Vector3D ToEuler(Quaternion q)
        {
            // black box content provided by:
            // https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles

            Vector3D angles = new Vector3D(0, 0, 0);

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = Math.CopySign(Math.PI / 2, sinp); // use 90 degrees if out of range
            else
                angles.Y = Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
        #endregion
        
        #region Texture Tab
        private void LoadTextureButton_Click(object sender, RoutedEventArgs e)
        {
            if(TileListBox.SelectedValue == null)
            {
                MessageBox.Show("Please first select a tile.", "Select tile", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Filter = "*.jpg|*.jpg|*.*|*.*";
            fileDialog.InitialDirectory = Environment.CurrentDirectory;
            if (fileDialog.ShowDialog() == true && File.Exists(fileDialog.FileName))
                LoadTexture(fileDialog.FileName, loadedTiles[(string)TileListBox.SelectedValue]);

            RefreshTextureutton_Click(null, new RoutedEventArgs());
        }

        private void RemoveTextureButton_Click(object sender, RoutedEventArgs e)
        {
            if (loadedTiles.ContainsKey((string)TileListBox.SelectedItem))
            {
                loadedTiles[(string)TileListBox.SelectedItem].Material = whiteMaterial;
                loadedTiles[(string)TileListBox.SelectedItem].BackMaterial = whiteMaterial;
            }
        }

        private void RefreshTextureutton_Click(object sender, RoutedEventArgs e)
        {
            if (TileListBox.SelectedValue != null)
            {
                if (loadedTiles[(string)TileListBox.SelectedValue].Material != whiteMaterial)
                {
                    TexturePreview.Source = ((ImageBrush)((DiffuseMaterial)loadedTiles[(string)TileListBox.SelectedValue].Material).Brush).ImageSource;
                }
            }
        }
        #endregion

        #region Lighting
        Color lightColor = Color.FromRgb(255, 255, 255);
        private void LightingColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender == RedSlider)
                lightColor.R = (byte)e.NewValue;
            if (sender == GreenSlider)
                lightColor.G = (byte)e.NewValue;
            if (sender == BlueSlider)
                lightColor.B = (byte)e.NewValue;

            RedValueLable.Content = lightColor.R;
            GreenValueLable.Content = lightColor.G;
            BlueValueLable.Content = lightColor.B;

            ((SolidColorBrush)ColorPreview.Fill).Color = lightColor;
            ambientLight.Color = lightColor;
        }
        #endregion

        private void TransformTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            Transform_LostFocus(null, new RoutedEventArgs());
        }

        private void TransformTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (double.TryParse(textBox.Text, out double value))
            {
                value += e.Delta > 0 ? 0.5 : -0.5;
                textBox.Text = value.ToString();
                Transform_LostFocus(null, new RoutedEventArgs());
            }
        }
    }

    class CameraController
    {
        public PerspectiveCamera camera;
        public Point3D center = new Point3D(0, 0, 0);
        Vector3D offset = new Vector3D(0, 0, 2);
        Quaternion rotation = Quaternion.Identity;
        double yaw = 0;
        double pitch = 0;
        double zoom = 2;

        public CameraController(PerspectiveCamera camera)
        {
            this.camera = camera;
        }

        public void Zoom(double amount)
        {
            zoom += -amount;
            if (zoom < 0) zoom = 0;
            offset = new Vector3D(0, 0, zoom);
            Update();
            //camera.Position += camera.LookDirection * amount;
        }

        public void Move(Vector3D movement)
        {
            Vector3D x = Vector3D.CrossProduct(center - camera.Position, Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection));
            Vector3D y = Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection);
            x.Normalize();
            y.Normalize();
            Vector3D move = x*(movement.Y * zoom) +  y*(movement.X * zoom);
            center += move;
            Update();
            //camera.Position += move;
            //new TranslateTransform3D(camera.UpDirection * movement.Y + Vector3D.CrossProduct(camera.UpDirection, camera.LookDirection) * movement.X);
            //camera.LookDirection = center - camera.Position;
        }

        public void Rotate(double yaw, double pitch)
        {
            this.yaw += yaw;
            this.pitch += pitch;
            if (this.pitch > 179) this.pitch = 179;
            if (this.pitch < 1) this.pitch = 1;
            Quaternion quatYaw = new Quaternion(new Vector3D(0, 0, 1), this.yaw);
            Quaternion quatPitch = new Quaternion(new Vector3D(1, 0, 0), this.pitch);
            rotation = quatYaw * quatPitch;
            Update();
            //camera.Transform = new RotateTransform3D(new AxisAngleRotation3D(rotation.Axis, rotation.Angle), center);
            //this.rotation *= rotation;
        }

        void Update()
        {
            if(!rotation.IsIdentity)
                camera.Position = center + new RotateTransform3D(new AxisAngleRotation3D(rotation.Axis, rotation.Angle), center).Transform(offset);
            camera.LookDirection = center - camera.Position;
            camera.UpDirection = new Vector3D(0, 0, 1);
            //if (camera.Position.X != double.NaN || camera.Position.Y == double.NaN || camera.Position.Z == double.NaN)
            //{
            //    camera.Position = new Point3D(0, 0, 0);
            //    center = new Point3D(0, 0, 0);
            //}
        }
    }
}
