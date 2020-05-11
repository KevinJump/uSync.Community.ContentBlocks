using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perplex.ContentBlocks.Definitions;
using Umbraco.Core;
using Umbraco.Core.Services;

using uSync8.ContentEdition.Mapping;
using uSync8.Core.Dependency;

namespace uSync.Community.ContentBlocks
{
    /// <summary>
    ///  value/dependency mapper for ContentBlocks
    /// </summary>
    /// <remarks>
    /// <para>
    ///  Content blocks contain nested content items, 
    ///  so for mapping/dependancy checking inside 
    ///  them to still work we need to go into the content blocks 
    ///  to get them.
    /// </para>
    /// <para>
    ///  Most of the time mapping doesn't matter (everything is GUIDs)
    ///  but we map into nested items to ensure dates are formatted correctly
    /// </para>
    /// <para>
    ///  dependencies matters more (for uSync.Publisher) as it allows us to 
    ///  build a full dependency tree including any media / linkied items inside
    ///  the content blocks
    /// </para>
    /// </remarks>
    /// <code>
    /// {
    /// "version": 2,
    /// "header": {
    ///     "id": "207257ac-808b-4d11-a061-47efccce178b",
    ///     "definitionId": "3cffc1a4-1359-4f99-8a23-9b61b4f9e969",
    ///     "layoutId": "de6adecf-2950-4fe6-b817-c3fd619f2fff",
    ///     "content": [
    ///         {
    ///         "key": "57356efa-7fd1-4064-ba51-66080cab99de",
    ///         "ncContentTypeAlias": "nestedContent",
    ///         "title": "Title",
    ///         "richText": "<p>Some title</p>",
    ///         "image": "umb://media/662af6ca411a4c93a6c722c4845698e7"
    ///         }
    ///     ]
    /// },
    /// "blocks": [... same as header ...]
    /// }
    /// </code>
    public class ContentBlocksMapper : SyncNestedValueMapperBase, ISyncMapper
    {
        private readonly IContentBlockDefinitionRepository definitionRepository;

        public ContentBlocksMapper(IEntityService entityService,
            IContentTypeService contentTypeService,
            IDataTypeService dataTypeService,
            IContentBlockDefinitionRepository definitionRepository)
            : base(entityService, contentTypeService, dataTypeService)
        {
            this.definitionRepository = definitionRepository;
        }

        public override string Name => "Content Blocks Mapper";
        public override string[] Editors => new string[] { "Perplex.ContentBlocks" };

        public override string GetExportValue(object value, string editorAlias)
        {
            if (value == null) return null;
            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return value.ToString();

            // check the version (we only support v2 as of today that is latest)
            var version = jsonValue.Value<int>("version");
            if (version != 2) return value.ToString();

            if (jsonValue.ContainsKey("header"))
            {
                jsonValue["header"] = GetBlocksExportValue(jsonValue["header"]);
            }

            if (jsonValue.ContainsKey("blocks"))
            {
                var blocks = jsonValue.Value<JArray>("blocks");
                for (int b = 0; b < blocks.Count; b++)
                {
                    blocks[b] = GetBlocksExportValue(blocks[b]);
                }
            }

            return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
        }

        public JToken GetBlocksExportValue(JToken blocks)
        {
            if (blocks == null) return blocks;

            if (blocks is JObject jBlocks)
            {
                if (!jBlocks.ContainsKey("content")) return blocks;
                var content = jBlocks.Value<JArray>("content");

                // pass the content json off to the NestedContent Value Mapper.
                var mappedValue = SyncValueMapperFactory.GetExportValue(content,
                    Constants.PropertyEditors.Aliases.NestedContent);

                jBlocks["content"] = JToken.Parse(mappedValue);
            }
            return blocks;
        }

        public override IEnumerable<uSyncDependency> GetDependencies(object value, string editorAlias, DependencyFlags flags)
        {
            if (value == null) return Enumerable.Empty<uSyncDependency>();
            var jsonValue = GetJsonValue(value);
            if (jsonValue == null) return Enumerable.Empty<uSyncDependency>();

            var dependencies = new List<uSyncDependency>();

            if (jsonValue.ContainsKey("header"))
            {
                dependencies.AddRange(GetBlocksDependencies(jsonValue["header"], flags));
            }

            if (jsonValue.ContainsKey("blocks"))
            {
                var blocks = jsonValue.Value<JArray>("blocks");
                for (int b = 0; b < blocks.Count; b++)
                {
                    dependencies.AddRange(GetBlocksDependencies(blocks[b], flags));
                }
            }

            return dependencies;
        }

        private IEnumerable<uSyncDependency> GetBlocksDependencies(JToken blocks, DependencyFlags flags)
        {
            List<uSyncDependency> dependencies = new List<uSyncDependency>();

            if (blocks is JObject jBlocks)
            {
                if (jBlocks.ContainsKey("definitionId"))
                {
                    var id = jBlocks.Value<string>("definitionId");

                    if (Guid.TryParse(id, out Guid key))
                    {
                        var definition = definitionRepository.GetById(key);
                        if (definition != null)
                        {
                            var dataType = dataTypeService.GetDataType(definition.DataTypeKey.Value);
                            if (dataType != null)
                            {
                                dependencies.Add(this.CreateDependency(dataType.GetUdi(), flags));
                            }
                        }
                    }
                }

                if (jBlocks.ContainsKey("content"))
                {
                    var content = jBlocks.Value<JArray>("content");
                    var mapper = SyncValueMapperFactory.GetMapper(Constants.PropertyEditors.Aliases.NestedContent);

                    dependencies.AddRange(mapper.GetDependencies(content,
                        Constants.PropertyEditors.Aliases.NestedContent,
                        flags));
                }
            }

            return dependencies;
        }
    }
}
