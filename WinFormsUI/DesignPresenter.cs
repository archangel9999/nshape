/******************************************************************************
  Copyright 2009 dataweb GmbH
  This file is part of the NShape framework.
  NShape is free software: you can redistribute it and/or modify it under the 
  terms of the GNU General Public License as published by the Free Software 
  Foundation, either version 3 of the License, or (at your option) any later 
  version.
  NShape is distributed in the hope that it will be useful, but WITHOUT ANY
  WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR 
  A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
  You should have received a copy of the GNU General Public License along with 
  NShape. If not, see <http://www.gnu.org/licenses/>.
******************************************************************************/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

using Dataweb.NShape.Advanced;
using Dataweb.NShape.Controllers;


namespace Dataweb.NShape.WinFormsUI {

	public partial class DesignPresenter : UserControl, IDisplayService {

		public DesignPresenter() {
			SetStyle(ControlStyles.ResizeRedraw
				| ControlStyles.AllPaintingInWmPaint
				| ControlStyles.OptimizedDoubleBuffer
				| ControlStyles.SupportsTransparentBackColor
				, true);
			UpdateStyles();

			// Initialize Components
			InitializeComponent();
			infoGraphics = Graphics.FromHwnd(Handle);
			
			//if (!this.project.SecurityManager.IsGranted(Permission.Designs)) {
			//   int cnt = Controls.Count;
			//   for (int i = 0; i < cnt; ++i)
			//      Controls[i].Enabled = false;
			//}
			//RegisterRepositoryEventHandlers();

			this.matrix = new Matrix();
			this.formatterFlags = 0 | StringFormatFlags.NoWrap;
			this.formatter = new StringFormat(formatterFlags);
			this.formatter.Trimming = StringTrimming.EllipsisCharacter;
			this.formatter.Alignment = StringAlignment.Center;
			this.formatter.LineAlignment = StringAlignment.Center;

			propertyGrid.Site = this.Site;
			styleListBox.BackColor = SelectedItemColor;
		}


		~DesignPresenter() {
			infoGraphics.Dispose();
			infoGraphics = null;
		}


		#region IDisplayService Members

		/// <override></override>
		void IDisplayService.Invalidate(int x, int y, int width, int height) { /* nothing to do */ }

		/// <override></override>
		void IDisplayService.Invalidate(Rectangle rectangle) { /* nothing to do */ }

		/// <override></override>
		void IDisplayService.NotifyBoundsChanged() { /* nothing to do */ }
		
		/// <override></override>
		Graphics IDisplayService.InfoGraphics {
			get { return infoGraphics; }
		}

		/// <override></override>
		IFillStyle IDisplayService.HintBackgroundStyle {
			get {
				if (Project != null && Project.IsOpen)
					return Project.Design.FillStyles.White;
				else return null;
			}
		}

		/// <override></override>
		ILineStyle IDisplayService.HintForegroundStyle {
			get {
				if (Project != null && Project.IsOpen)
					return Project.Design.LineStyles.Normal;
				else return null;
			}
		}

		#endregion


		#region [Public] Events

		public event EventHandler DesignSelected;

		public event EventHandler StyleSelected;

		#endregion


		#region [Public] Properties: DesignPresenter

		/// <summary>
		/// Specifies the version of the component.
		/// </summary>
		[Category("NShape")]
		[Browsable(true)]
		public new string ProductVersion {
			get { return base.ProductVersion; }
		}


		[Category("NShape")]
		public Project Project {
			get { return (designController == null) ? null : designController.Project; }
		}


		[Category("NShape")]
		public DesignController DesignController {
			get { return designController; }
			set {
				if (designController != null) UnregisterDesignControllerEventHandlers();
				designController = value;
				if (designController != null) RegisterDesignControllerEventHandlers();
			}
		}


		[Browsable(false)]
		public Design SelectedDesign {
			get { return selectedDesign; }
			set {
				if (selectedDesign != value) {
					SelectedStyle = null;
					selectedDesign = value;
					
					StyleEditor.Design = selectedDesign;
					InitializeStyleCollectionList();
					if (DesignSelected != null) DesignSelected(this, eventArgs);
				}
			}
		}


		[Browsable(false)]
		public Style SelectedStyle {
			get { return selectedStyle; }
			private set {
				if (selectedStyle != value) {
					selectedStyle = value;
					if (propertyController != null) propertyController.SetObject(0, selectedStyle);
					if (StyleSelected != null) StyleSelected(this, eventArgs);
				}
			}
		}

		#endregion


		#region [Public] Properties: Visuals

		public Color InactiveItemBackgroundColor {
			get {
				return backgroundColor;
			}
			set {
				if (backgroundBrush != null) {
					backgroundBrush.Dispose();
					backgroundBrush = null;
				}
				backgroundColor = value;
			}
		}


		public Color HighlightedItemColor {
			get { return highlightedItemColor; }
			set {
				if (highlightedItemBrush != null) {
					highlightedItemBrush.Dispose();
					highlightedItemBrush = null;
				}
				highlightedItemColor = value;
			}
		}


		public Color SelectedItemColor {
			get { return selectedItemColor; }
			set {
				if (selectedItemBrush != null) {
					selectedItemBrush.Dispose();
					selectedItemBrush = null;
				}
				selectedItemColor = value;
			}
		}


		public Color InactiveItemBorderColor {
			get { return itemBorderColor; }
			set {
				if (itemBorderPen != null) {
					itemBorderPen.Dispose();
					itemBorderPen = null;
				}
				itemBorderColor = value;
			}
		}


		public Color FocusedItemColor {
			get { return focusBackgroundColor; }
			set {
				if (focusBackgroundBrush != null) {
					focusBackgroundBrush.Dispose();
					focusBackgroundBrush = null;
				}
				focusBackgroundColor = value;
			}
		}


		public Color FocusBorderColor {
			get { return focusBorderColor; }
			set {
				if (selectedBorderPen != null) {
					selectedBorderPen.Dispose();
					selectedBorderPen = null;
				}
				focusBorderColor = value;
			}
		}


		public Color SelectedItemTextColor {
			get { return selectedTextColor; }
			set {
				if (selectedTextBrush != null) {
					selectedTextBrush.Dispose();
					selectedTextBrush = null;
				}
				selectedTextColor = value;
			}
		}


		public Color InactiveItemTextColor {
			get { return itemTextColor; }
			set {
				if (itemTextBrush != null) {
					itemTextBrush.Dispose();
					itemTextBrush = null;
				}
				itemTextColor = value;
			}
		}


		public bool HighlightItems {
			get { return highlightItems; }
			set { highlightItems = value; }
		}

		#endregion


		#region [Public] Methods

		public void CreateDesign() {
			designController.CreateDesign();
		}


		public void DeleteSelectedDesign() {
			if (selectedDesign != Project.Design) {
				designController.DeleteDesign(selectedDesign);
				SelectedDesign = Project.Design;
			}
		}


		public void CreateStyle() {
			designController.CreateStyle(selectedDesign, styleListBox.StyleCategory);
		}


		public void DeleteSelectedStyle() {
			designController.DeleteStyle(selectedDesign, selectedStyle);
		}

		#endregion


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}


		private void InitializeStyleCollectionList() {
			styleCollectionListBox.Items.Clear();
			styleCollectionListBox.Items.Insert(colorStylesItemIdx, "Color Styles");
			styleCollectionListBox.Items.Insert(fillStylesItemIdx, "Fill Styles");
			styleCollectionListBox.Items.Insert(lineStylesItemIdx, "Line Styles");
			styleCollectionListBox.Items.Insert(capStylesItemIdx, "Line Cap Styles");
			styleCollectionListBox.Items.Insert(charStylesItemIdx, "Character Styles");
			styleCollectionListBox.Items.Insert(paragraphStylesItemIdx, "Paragraph Styles");
			//styleCollectionListBox.Items.Insert(shapeStylesItemIdx, "Shape Styles");

			styleCollectionListBox.SelectedIndex = 0;
			styleCollectionListBox.Invalidate();
		}


		#region [Private] Methods: (Un)Register events

		private void RegisterDesignControllerEventHandlers() {
			if (designController != null) {
				designController.Initialized += designController_Initialized;
				designController.Uninitialized += designController_Uninitialized;
				designController.DesignCreated += designController_DesignCreated;
				designController.DesignChanged += designController_DesignChanged;
				designController.DesignDeleted += designController_DesignDeleted;
				designController.StyleCreated += designController_StyleCreated;
				designController.StyleChanged += designController_StyleChanged;
				designController.StyleDeleted += designController_StyleDeleted;
			}
		}


		private void UnregisterDesignControllerEventHandlers() {
			if (designController != null) {
				designController.Initialized -= designController_Initialized;
				designController.Uninitialized -= designController_Uninitialized;
				designController.DesignCreated -= designController_DesignCreated;
				designController.DesignChanged -= designController_DesignChanged;
				designController.DesignDeleted -= designController_DesignDeleted;
				designController.StyleCreated -= designController_StyleCreated;
				designController.StyleChanged -= designController_StyleChanged;
				designController.StyleDeleted -= designController_StyleDeleted;
			}
		}

		#endregion


		#region [Private] Methods: DesignController event handler implementations

		private void designController_Initialized(object sender, EventArgs e) {
			propertyController.Project = Project;
			InitializeStyleCollectionList();
			SelectedDesign = designController.Project.Design;
		}


		private void designController_Uninitialized(object sender, EventArgs e) {
			selectedStyle = null;
			selectedDesign = null;
			styleListBox.Items.Clear();
			propertyController.Project = null;
		}


		private void designController_StyleCreated(object sender, StyleEventArgs e) {
			if (styleListBox.Design.ContainsStyle(e.Style)) {
				styleListBox.SuspendLayout();
				styleListBox.SelectedIndex = styleListBox.Items.Add(e.Style);
				styleListBox.ResumeLayout();
			}
		}


		private void designController_StyleChanged(object sender, StyleEventArgs e) {
			int idx = styleListBox.Items.IndexOf(e.Style);
			if (idx >= 0) {
				bool isSelectedStyle = (styleListBox.SelectedItem == e.Style);
				if (e.Style is ICapStyle || e.Style is ICharacterStyle || e.Style is ILineStyle) {
					styleListBox.Items.RemoveAt(idx);
					styleListBox.Items.Insert(idx, e.Style);
					styleListBox.SelectedIndex = idx;
				} else styleListBox.Invalidate();
			}

			StyleEditor.Design = e.Design;
			if (propertyGrid.SelectedObject == e.Style)
				propertyGrid.Refresh();
		}


		private void designController_StyleDeleted(object sender, StyleEventArgs e) {
			if (styleListBox.Items.Contains(e.Style)) {
				propertyGrid.SuspendLayout();
				styleListBox.SuspendLayout();

				if (propertyController != null) {
					if (propertyController.GetSelectedObject(0) == e.Style)
						propertyController.SetObject(0, null);
				}
				// remove deleted item and select the previous one
				int idx = styleListBox.Items.IndexOf(e.Style);
				styleListBox.Items.RemoveAt(idx);
				--idx;
				if (idx < 0 && styleListBox.Items.Count > 0)
					idx = 0;
				styleListBox.SelectedIndex = idx;

				styleListBox.ResumeLayout();
				propertyGrid.ResumeLayout();
			}
		}


		private void designController_DesignCreated(object sender, DesignEventArgs e) {
			// nothing to do
		}


		private void designController_DesignChanged(object sender, DesignEventArgs e) {
			// nothing to do
		}


		private void designController_DesignDeleted(object sender, DesignEventArgs e) {
			// nothing to do
		}

		#endregion


		#region [Private] Methods: Event handler implementations

		private void styleCollectionListBox_SelectedIndexChanged(object sender, System.EventArgs e) {
			//propertyGrid.SelectedObject = null;
			if (propertyController != null) propertyController.SetObject(0, null);
			//
			styleListBox.SuspendLayout();
			styleListBox.SelectedItem = null;
			styleListBox.Items.Clear();
			// Assigning a Design here results in items beeing created
			styleListBox.Design = selectedDesign;
			switch (styleCollectionListBox.SelectedIndex) {
				case -1:
					//nothing to do
					break;
				case capStylesItemIdx: styleListBox.StyleCategory = StyleCategory.CapStyle; break;
				case charStylesItemIdx: styleListBox.StyleCategory = StyleCategory.CharacterStyle; break;
				case colorStylesItemIdx: styleListBox.StyleCategory = StyleCategory.ColorStyle; break;
				case fillStylesItemIdx: styleListBox.StyleCategory = StyleCategory.FillStyle; break;
				case lineStylesItemIdx: styleListBox.StyleCategory = StyleCategory.LineStyle; break;
				case paragraphStylesItemIdx: styleListBox.StyleCategory = StyleCategory.ParagraphStyle; break;
				//case shapeStylesItemIdx: styleListBox.StyleCategory = StyleCategory.ShapeStyle; break;
				default: throw new NShapeException("Unexpected value.");
			}
			if (styleListBox.Items.Count > 0) styleListBox.SelectedIndex = 0;
		}


		private void styleListBox_SelectedIndexChanged(object sender, EventArgs e) {
			SelectedStyle = styleListBox.SelectedStyle;
		}

	
		private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e) {
		}


		public void ActivateDesign(Design design) {
			if (Project.Design != design) Project.ApplyDesign(design);
		}
		
		#endregion


		#region Fields
		private Graphics infoGraphics;

		private DesignController designController = null;
		private Design selectedDesign = null;
		private Style selectedStyle = null;

		// EventArgs buffer
		private EventArgs eventArgs = new EventArgs();

		private bool highlightItems = true;
		// Colors
		private Color backgroundColor = Color.FromKnownColor(KnownColor.Control);
		private Color highlightedItemColor = Color.FromKnownColor(KnownColor.ControlLightLight);
		private Color selectedItemColor = Color.FromKnownColor(KnownColor.Window);
		private Color selectedItemBorderColor = Color.FromKnownColor(KnownColor.ControlDarkDark);
		private Color itemBorderColor = Color.FromKnownColor(KnownColor.Window);
		private Color focusBackgroundColor = Color.Beige;
		private Color focusBorderColor = Color.FromArgb(128, Color.Beige);
		private Color itemTextColor = Color.FromKnownColor(KnownColor.ControlDarkDark);
		private Color selectedTextColor = Color.FromKnownColor(KnownColor.ControlText);
		// Pens and Brushes
		private Brush backgroundBrush;
		private Brush highlightedItemBrush;
		private Brush selectedItemBrush;
		private Brush itemTextBrush;
		private Brush selectedTextBrush;
		private Brush focusBackgroundBrush;
		private Pen itemBorderPen;
		private Pen selectedBorderPen;
		// Buffers
		private Matrix matrix;
		private StringFormat formatter;
		private StringFormatFlags formatterFlags;

		const int noneSelectedItemIdx = -1;
		const int colorStylesItemIdx = 0;
		const int fillStylesItemIdx = 1;
		const int lineStylesItemIdx = 2;
		const int capStylesItemIdx = 3;
		const int charStylesItemIdx = 4;
		const int paragraphStylesItemIdx = 5;
		const int shapeStylesItemIdx = 6;
		#endregion
	}
}