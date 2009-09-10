using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;


namespace Dataweb.nShape.Advanced {

	/// <summary>
	/// Provides a set of styles.
	/// </summary>
	/// <status>reviewed</status>
	public interface IStyleSetProvider {
		/// <summary>
		/// Retrives a style set.
		/// </summary>
		IStyleSet StyleSet { get; }
	}


	/// <summary>
	/// Represents a method that creates a shape with or without template.
	/// </summary>
	/// <param name="shapeType">Shape type of the new shape.</param>
	/// <param name="template">Template of the new shape. If null a self-contained shape is created.</param>
	/// <status>reviewed</status>
	public delegate Shape CreateShapeDelegate(ShapeType shapeType, Template template);


	/// <summary>
	/// Describes a shape type.
	/// </summary>
	public sealed class ShapeType {

		/// <summary>
		/// Constructs a shape type.
		/// </summary>
		public ShapeType(string name, string libraryName, ResourceString categoryTitle,
			CreateShapeDelegate createShapeDelegate, GetPropertyDefinitionsDelegate getPropertyDefinitionsDelegate)
			: this(name, libraryName, categoryTitle, createShapeDelegate, getPropertyDefinitionsDelegate, null, true) {
		}


		/// <summary>
		/// Constructs a shape type.
		/// </summary>
		public ShapeType(string name, string libraryName, ResourceString categoryTitle,
			CreateShapeDelegate createShapeDelegate, GetPropertyDefinitionsDelegate getPropertyDefinitionsDelegate,
			Bitmap freehandReferenceImage)
			: this(name, libraryName, categoryTitle, createShapeDelegate, getPropertyDefinitionsDelegate, freehandReferenceImage, true) {
		}


		/// <summary>
		/// Constructs a shape type.
		/// </summary>
		public ShapeType(string name, string libraryName, ResourceString categoryTitle,
			CreateShapeDelegate createShapeDelegate, GetPropertyDefinitionsDelegate getPropertyDefinitionsDelegate,
			bool supportsTemplates)
			: this(name, libraryName, categoryTitle, createShapeDelegate, getPropertyDefinitionsDelegate, null, supportsTemplates) {
		}


		/// <summary>
		/// Constructs a shape type.
		/// </summary>
		public ShapeType(string name, string libraryName, ResourceString categoryTitle,
			CreateShapeDelegate createShapeDelegate, GetPropertyDefinitionsDelegate getPropertyDefinitionsDelegate,
			Bitmap freehandReferenceImage, bool supportsTemplates) {
			// Sanity check
			if (name == null) throw new ArgumentNullException("Shape type name");
			if (!Project.IsValidName(name))
				throw new ArgumentException(string.Format("'{0}' is not a valid shape type name.", name));
			if (libraryName == null) throw new ArgumentNullException("Namespace name");
			if (!Project.IsValidName(libraryName))
				throw new ArgumentException("'{0}' is not a valid library name.", libraryName);
			if (createShapeDelegate == null) throw new ArgumentNullException("Shape creation delegate");
			if (getPropertyDefinitionsDelegate == null) throw new ArgumentNullException("Property infos");
			//
			this.name = name;
			this.libraryName = libraryName;
			this.categoryTitle = categoryTitle;
			this.createShapeDelegate = createShapeDelegate;
			this.getPropertyDefinitionsDelegate = getPropertyDefinitionsDelegate;
			this.freehandReferenceImage = freehandReferenceImage;
			this.supportsAutoTemplates = supportsTemplates;
		}


		/// <summary>
		/// Name of shape type, used to identify a shape type for creating shapes.
		/// </summary>
		public string Name { 
			get { return name; } 
		}


		/// <summary>
		/// Indicates the name of the library this shape type is implemented in.
		/// </summary>
		public string LibraryName {
			get { return libraryName; }
		}


		/// <summary>
		/// Indicates the complete unique name of the shape type.
		/// </summary>
		public string FullName {
			get { return string.Format("{0}.{1}", libraryName, name); }
		}


		/// <summary>
		/// Indicates the default for the culture depending category name.
		/// </summary>
		public string DefaultCategoryTitle { 
			get { return categoryTitle.Value; } 
		}


		/// <summary>
		/// Specifies the culture depending description of the model type.
		/// </summary>
		public string Description {
			get { return description.Value; }
			set { description = value; }
		}


		/// <summary>
		/// Indicates whether it makes sense to create an automatic template for this 
		/// shape type.
		/// </summary>
		public bool SupportsAutoTemplates {
			get { return supportsAutoTemplates; }
		}


		/// <summary>
		/// Indicates the pattern bitmap for freehand drawing.
		/// </summary>
		/// <returns></returns>
		public Bitmap FreehandReferenceImage {
			get { return freehandReferenceImage; }
		}


		/// <summary>
		/// Create a completely new shape instance which will be initialized with the standard styles.
		/// </summary>
		public Shape CreateInstance() {
			if (styleSetProvider == null)
				throw new InvalidOperationException(string.Format("No style set provider found for shape type '{0}'. "
				+ "Probably the shape type is not registered with the project.", FullName));
			Shape result = createShapeDelegate(this, null);
			result.InitializeToDefault(styleSetProvider.StyleSet);
			return result;
		}


		/// <summary>
		/// Create a new shape based on a template. The shape will use the template's styles.
		/// </summary>
		public Shape CreateInstance(Template template) {
			if (template == null) throw new ArgumentNullException("template");
			Shape result = createShapeDelegate(this, template);
			result.CopyFrom(template.Shape);
			return result;
		}


		/// <summary>
		/// Creates an exact clone of the given shape that uses preview styles.
		/// </summary>
		public Shape CreatePreviewInstance(Shape shape) {
			if (shape == null) throw new ArgumentNullException("shape");
			Shape result = shape.Clone();
			result.MakePreview(styleSetProvider.StyleSet);
			return result;
		}


		/// <summary>
		/// Retrieves the property definitions of the shape type.
		/// </summary>
		/// <param name="version">Repository version for which the property definitions 
		/// are to be fetched.</param>
		/// <returns></returns>
		public IEnumerable<EntityPropertyDefinition> GetPropertyDefinitions(int version) {
			return getPropertyDefinitionsDelegate(version);
		}


		/// <summary>
		/// Converts the value of this instance to a string.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return FullName;
		}


		/// <summary>
		/// Used by the project to supplement a design.
		/// </summary>
		internal IStyleSetProvider StyleSetProvider {
			get { return styleSetProvider; }
			set { styleSetProvider = value; }
		}


		/// <summary>
		/// Creates an empty instance for subsequent loading from a repository.
		/// </summary>
		/// <returns></returns>
		internal Shape CreateInstanceForLoading() {
			return this.createShapeDelegate(this, null);
		}


		#region Fields

		// Name of the shape type
		private string name;
		// Name of the shape type's library
		private string libraryName;
		// Localizable default for the shape type category e.g. in the toolbox.
		private ResourceString categoryTitle;
		// Localizable description for the shape type.
		private ResourceString description;
		// StyleSetProvider for retrieving the current StyleSet
		private IStyleSetProvider styleSetProvider;
		// Delegate for constructing shapes
		private CreateShapeDelegate createShapeDelegate;
		// Delegate for fetching the shape type definitions.
		private GetPropertyDefinitionsDelegate getPropertyDefinitionsDelegate;
		// Image used by the FreeHandTool to identify a drawn shape
		private Bitmap freehandReferenceImage = null;
		// True, if automatic templates make sense for this shape type
		bool supportsAutoTemplates = true;

		#endregion
	}


	/// <summary>
	/// Provides reading access to a collection of shape types.
	/// </summary>
	public interface IReadOnlyShapeTypeCollection : IReadOnlyCollection<ShapeType> {

		ShapeType this[string shapeTypeName] { get; }

	}


	/// <summary>
	/// Manages a list of shape types.
	/// </summary>
	internal class ShapeTypeCollection : IReadOnlyShapeTypeCollection {

		internal ShapeTypeCollection() {
		}


		/// <summary>
		/// Adds a shape type to the collection.
		/// </summary>
		/// <param name="shapeType"></param>
		public void Add(ShapeType shapeType) {
			if (shapeType == null) throw new ArgumentNullException("shapeType");
			shapeTypes.Add(shapeType.FullName, shapeType);
		}


		public ShapeType this[string shapeTypeName] {
			get { return GetShapeType(shapeTypeName); }
		}


		public int Count {
			get { return shapeTypes.Count; }
		}


		public void Clear() {
			shapeTypes.Clear();
		}


		#region IEnumerable<Type> Members

		public IEnumerator<ShapeType> GetEnumerator() {
			return shapeTypes.Values.GetEnumerator();
		}

		#endregion


		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			return shapeTypes.Values.GetEnumerator();
		}

		#endregion


		#region ICollection Members

		public void CopyTo(Array array, int index) {
			if (array == null) throw new ArgumentNullException("array");
			shapeTypes.Values.CopyTo((ShapeType[])array, index);
		}


		public bool IsSynchronized {
			get { return false; }
		}


		public object SyncRoot {
			get { throw new NotImplementedException(); }
		}

		#endregion


		/// <summary>
		/// Retrieves the shape type with the given projectName.
		/// </summary>
		/// <param name="typeName">Either a full (i.e. including the namespace) 
		/// or partial shape type projectName</param>
		/// <returns>Shape type with given projectName.</returns>
		protected ShapeType GetShapeType(string typeName) {
			ShapeType result = null;
			if (!shapeTypes.TryGetValue(typeName, out result)) {
				foreach (KeyValuePair<string, ShapeType> item in shapeTypes) {
					// if no matching type projectName was found, check if the given type projectName was a type projectName without namespace
					if (item.Value.Name == typeName) {
						if (result == null) result = item.Value;
						else throw new ArgumentException("The shape type '{0}' is ambiguous. Please specify the library name.", typeName);
					}
				}
			}
			if (result == null)
				throw new ArgumentException(string.Format("Shape type '{0}' was not registered.", typeName));
			return result;
		}


		#region Fields

		// Files shape types under their names.
		private Dictionary<string, ShapeType> shapeTypes 
			= new Dictionary<string, ShapeType>(StringComparer.InvariantCultureIgnoreCase);

		#endregion

	}

}