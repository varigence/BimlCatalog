using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using System.Security.Cryptography;

namespace Varigence.Ssis
{
    [
        DtsPipelineComponent
        (
            DisplayName = "Biml Hash Dual"
            , Description = "Dual SHA1 hash columns and reverse of columns."
            , ComponentType = ComponentType.Transform
            , IconResource = "Varigence.Ssis.Icons.HashDual.ico"
        )]
    public class HashDual : PipelineComponent
    {
        #region Member data

        private struct ColumnInfo
        {

            public int bufferColumnIndex;
            public DTSRowDisposition columnDisposition;
            public int lineageID;
            public string dataType;
        }

        /// <summary>
        /// Used when directing rows to the error output. The integer specifies the error code for the column.
        /// The string specifies the message displayed for the error code.
        /// </summary>

        const int INVALID_CHARACTER_INDEX = 0x1;
        const string InvalidCharacterIndexmessage = "The character index to change is outside the length of the column value.";
        private static SHA1 _sha1 = SHA1.Create();
        private const Boolean millisecondHandling = true;
        //        private HashDualAlgorithm HashDualSHA1Algorithm;
        private ColumnInfo[] inputColumnInfos;
        private ColumnInfo[] outputColumnInfos;
        internal const string HashDual_ALGORITHM_PROPERTY = "HashDualAlgorithm";

        #endregion

        #region Design Time

        #region PerformUpgrade
        public override void PerformUpgrade(int pipelineVersion)
        {
            int componentVersion = ((DtsPipelineComponentAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DtsPipelineComponentAttribute), false)).CurrentVersion;
            ComponentMetaData.Version = componentVersion;
            IDTSOutput100 output = base.ComponentMetaData.OutputCollection[0];

            if (output.OutputColumnCollection.Count == 1)
            {
                IDTSOutputColumn100 column = output.OutputColumnCollection[0];

                if (column.CustomPropertyCollection.Count == 0)
                {
                    IDTSCustomProperty100 property = column.CustomPropertyCollection.New();
                    property.Name = "HashDual";
                    property.Description = "HashDual SHA1 transformation result column indicator.";
                    property.Value = "HashDual";
                }
            }
        }
        #endregion

        #region ProvideComponentProperties
        /// <summary>
        /// Called when the component is initially added to the data flow task. Add the input, output, and error output.
        /// </summary>
        public override void ProvideComponentProperties()
        {
            RemoveAllInputsOutputsAndCustomProperties();
            ComponentMetaData.UsesDispositions = true;

            //	Add the input
            var input = ComponentMetaData.InputCollection.New();
            input.Name = "HashDualInput";
            input.ErrorRowDisposition = DTSRowDisposition.RD_FailComponent;

            //	Add the output
            var output = ComponentMetaData.OutputCollection.New();
            output.Name = "HashDualOutput";
            output.SynchronousInputID = input.ID;
            output.ExclusionGroup = 1;

            AddHashDualColumn("Hash1");
            AddHashDualColumn("Hash2");
        }
        #endregion

        #region AddHashDualColumn
        private void AddHashDualColumn(string columnName)
        {
            IDTSOutputColumn100 column = base.ComponentMetaData.OutputCollection[0].OutputColumnCollection.New();
            column.Name = columnName;
            column.SetDataTypeProperties(DataType.DT_STR, 40, 0, 0, 1252);
            IDTSCustomProperty100 property = column.CustomPropertyCollection.New();
            property.Name = columnName;
            property.Description = "HashDual SHA1 transformation result column indicator.";
            property.Value = columnName;
        }

        #endregion

        #region Validate
        public override DTSValidationStatus Validate()
        {
            ///	If there is an input column that no longer exists in the Virtual input collection,
            /// return needs new meta data. The designer will then call ReinitalizeMetadata which will clean up the input collection.
            if (ComponentMetaData.AreInputColumnsValid == false)
            {
                return DTSValidationStatus.VS_NEEDSNEWMETADATA;
            }

            if (ValidateOutputColumn()) return base.Validate();
            ComponentMetaData.FireWarning(0, ComponentMetaData.Name, "The output column collection is invalid and will need to be reset.", "", 0);
            return DTSValidationStatus.VS_ISBROKEN;
        }

        private bool ValidateOutputColumn()
        {
            IDTSOutput100 output = base.ComponentMetaData.OutputCollection[0];
            return (((output.OutputColumnCollection.Count == 2)
                && (output.OutputColumnCollection[0].DataType == DataType.DT_STR))
                && ((output.OutputColumnCollection[0].CustomPropertyCollection.Count == 1)
                && (output.OutputColumnCollection[0].CustomPropertyCollection[0].Name == "Hash1")
                && (output.OutputColumnCollection[1].DataType == DataType.DT_STR))
                && ((output.OutputColumnCollection[1].CustomPropertyCollection.Count == 1)
                && (output.OutputColumnCollection[1].CustomPropertyCollection[0].Name == "Hash2")
                ));
        }
        #endregion

        #region ReinitializeMetaData
        /// <summary>
        /// Called after the component has returned VS_NEEDSNEWMETADATA from Validate. Removes any input columns that 
        /// no longer exist in the Virtual Input Collection.
        /// </summary>
        public override void ReinitializeMetaData()
        {
            ComponentMetaData.RemoveInvalidInputColumns();
            base.ReinitializeMetaData();
            IDTSOutput100 output = base.ComponentMetaData.OutputCollection[0];
            if (ValidateOutputColumn()) return;
            output.OutputColumnCollection.RemoveAll();
            AddHashDualColumn("Hash1");
            AddHashDualColumn("Hash2");
        }
        #endregion


        #region SetUsageType
        /// <summary>
        /// Called when a user has selected an Input column for the component. This component only accepts input columns
        /// that have DTSUsageType.UT_READWRITE. Any other usage types are rejected.
        /// </summary>
        /// <param name="inputID">The ID of the input that the column is inserted in.</param>
        /// <param name="virtualInput">The virtual input object containing that contains the new column.</param>
        /// <param name="lineageID">The lineageID of the virtual input column.</param>
        /// <param name="usageType">The DTSUsageType parameter that specifies how the column is used by the component.</param>
        /// <returns>The newly created IDTSInputColumn100.</returns>
        public override IDTSInputColumn100 SetUsageType(int inputID, IDTSVirtualInput100 virtualInput, int lineageID, DTSUsageType usageType)
        {
            IDTSVirtualInputColumn100 virtualColumn = virtualInput.VirtualInputColumnCollection.GetVirtualInputColumnByLineageID(lineageID);
            if (usageType == DTSUsageType.UT_READWRITE)
                throw new Exception("The UsageType must be set to Read Only.");

            //	Get the column
            IDTSInputColumn100 col = base.SetUsageType(inputID, virtualInput, lineageID, usageType);

            //if (((virtualColumn.DataType != DataType.DT_NTEXT)
            //    && (virtualColumn.DataType != DataType.DT_IMAGE))
            //    && (virtualColumn.DataType != DataType.DT_BYTES))
            //{
                return base.SetUsageType(inputID, virtualInput, lineageID, usageType);
            //}
            //throw new Exception("Input column data types cannot be DT_NTEXT, D_IMAGE or DT_BYTES.");
        }
        #endregion

        #region DeleteOutput
        /// <summary>
        /// Called when an IDTSOutput100 is deleted from the component. Disallow outputs to be deleted by throwing an exception.
        /// </summary>
        /// <param name="outputID">The ID of the output to delete.</param>
        public override void DeleteOutput(int outputID)
        {
            throw new Exception("Can't delete output " + outputID.ToString(CultureInfo.InvariantCulture));
        }
        #endregion

        #region InsertOutput
        /// <summary>
        /// Called when an IDTSOutput100 is added to the component. Disallow new outputs by throwing an exception.
        /// </summary>
        /// <param name="insertPlacement">The location, relative to the output specified by outputID,to insert the new output.</param>
        /// <param name="outputID">The ID of the output that the new output is located next to.</param>
        /// <returns></returns>
        public override IDTSOutput100 InsertOutput(DTSInsertPlacement insertPlacement, int outputID)
        {
            throw new Exception("Can't add output to the component.");
        }
        #endregion

        #endregion

        #region Runtime

        #region PreExecute
        /// <summary>
        /// Called prior to ProcessInput, the buffer column index, index of the character to change, and the operation
        /// for each column in the input collection is read, and stored.
        /// </summary>
        public override void PreExecute()
        {

            //bool flag = false;
            IDTSInput100 input = base.ComponentMetaData.InputCollection[0];
            inputColumnInfos = new ColumnInfo[input.InputColumnCollection.Count];
            for (var i = 0; i < input.InputColumnCollection.Count; i++)
            {
                IDTSInputColumn100 column = input.InputColumnCollection[i];
                inputColumnInfos[i] = new ColumnInfo
                {
                    bufferColumnIndex = BufferManager.FindColumnByLineageID(input.Buffer, column.LineageID),
                    columnDisposition = column.ErrorRowDisposition,
                    lineageID = column.LineageID,
                    dataType = column.DataType.ToString()
                };
            }
            IDTSOutput100 output = base.ComponentMetaData.OutputCollection[0];
            outputColumnInfos = new ColumnInfo[output.OutputColumnCollection.Count];
            for (var j = 0; j < output.OutputColumnCollection.Count; j++)
            {
                IDTSOutputColumn100 column2 = output.OutputColumnCollection[j];
                outputColumnInfos[j] = new ColumnInfo
                {
                    bufferColumnIndex = BufferManager.FindColumnByLineageID(input.Buffer, column2.LineageID),
                    columnDisposition = column2.ErrorRowDisposition,
                    lineageID = column2.LineageID,
                    dataType = column2.DataType.ToString()
                };
            }

            //IDTSCustomProperty100 HashDualSHA1Algorithm = ComponentMetaData.CustomPropertyCollection["HashDual"];
        }
        #endregion

        #region OnInputPathAttached
        public override void OnInputPathAttached(int inputID)
        {
            //IDTSVirtualInput100 virtualInput = ComponentMetaData.InputCollection.GetObjectByID(inputID).GetVirtualInput();
            //foreach (IDTSVirtualInputColumn100 column in virtualInput.VirtualInputColumnCollection)
            //{
            //    if (column.Name != "ErrorColumn")
            //    {
            //        SetUsageType(inputID, virtualInput, column.LineageID, 0);
            //    }
            //}
        }
        #endregion

        #region ProcessInput
        /// <summary>
        /// Called when a PipelineBuffer is passed to the component.
        /// </summary>
        /// <param name="inputID">The ID of the Input that the buffer contains rows for.</param>
        /// <param name="buffer">The PipelineBuffer containing the columns defined in the IDTSInput100.</param>
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (!buffer.EndOfRowset)
            {
                IDTSInput100 input = ComponentMetaData.InputCollection.GetObjectByID(inputID);

                var errorOutputID = -1;
                var errorOutputIndex = -1;
                var defaultOutputId = -1;

                GetErrorOutputInfo(ref errorOutputID, ref errorOutputIndex);


                defaultOutputId = errorOutputIndex == 0 ? ComponentMetaData.OutputCollection[1].ID : ComponentMetaData.OutputCollection[0].ID;


                while (buffer.NextRow())
                {
                    /// If the inputColumnInfos array has zero dimensions, then 
                    /// no input columns have been selected for the component. 
                    /// Direct the row to the default output.
                    if (inputColumnInfos.Length == 0)
                    {
                        buffer.DirectRow(defaultOutputId);
                    }
                    else
                    {
                        var isError = false;
                        var inputByteBuffer = new byte[1000];
                        var bufferUsed = 0;
                        var nullHandling = String.Empty;

                        foreach (var columnToProcessID in inputColumnInfos.Select(info => info.bufferColumnIndex))
                        {
                            if (!buffer.IsNull(columnToProcessID))
                            {
                                nullHandling += "N";
                                switch (buffer.GetColumnInfo(columnToProcessID).DataType)
                                {
                                    case DataType.DT_BOOL:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetBoolean(columnToProcessID));
                                        break;
                                    case DataType.DT_IMAGE:
                                        uint blobLength = buffer.GetBlobLength(columnToProcessID);
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetBlobData(columnToProcessID, 0, (int)blobLength));
                                        nullHandling += blobLength.ToString(CultureInfo.InvariantCulture);
                                        break;
                                    case DataType.DT_BYTES:
                                        byte[] bytesFromBuffer = buffer.GetBytes(columnToProcessID);
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, bytesFromBuffer);
                                        nullHandling += bytesFromBuffer.GetLength(0).ToString(CultureInfo.InvariantCulture);
                                        break;
                                    case DataType.DT_CY:
                                    case DataType.DT_DECIMAL:
                                    case DataType.DT_NUMERIC:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetDecimal(columnToProcessID));
                                        break;
//#if SQL2005
//#else
                                        //case DataType.DT_DBTIMESTAMPOFFSET:
                                        //    DateTimeOffset dateTimeOffset = buffer.GetDateTimeOffset(columnToProcessID);
                                        //    Utility.Append(ref inputByteBuffer, ref bufferUsed, dateTimeOffset);
                                        //    break;
                                    case DataType.DT_DBDATE:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetDate(columnToProcessID), millisecondHandling);
                                        break;
//#endif
                                    case DataType.DT_DATE:
                                    case DataType.DT_DBTIMESTAMP:
#if SQL2005
#else
                                    case DataType.DT_DBTIMESTAMP2:
                                    case DataType.DT_FILETIME:
#endif
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetDateTime(columnToProcessID), millisecondHandling);
                                        break;
#if SQL2005
#else
                                    case DataType.DT_DBTIME:
                                    case DataType.DT_DBTIME2:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetTime(columnToProcessID));
                                        break;
#endif
                                    case DataType.DT_GUID:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetGuid(columnToProcessID));
                                        break;
                                    case DataType.DT_I1:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetSByte(columnToProcessID));
                                        break;
                                    case DataType.DT_I2:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetInt16(columnToProcessID));
                                        break;
                                    case DataType.DT_I4:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetInt32(columnToProcessID));
                                        break;
                                    case DataType.DT_I8:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetInt64(columnToProcessID));
                                        break;
                                    case DataType.DT_NTEXT:
                                    case DataType.DT_STR:
                                    case DataType.DT_TEXT:
                                    case DataType.DT_WSTR:
                                        String stringFromBuffer = buffer.GetString(columnToProcessID);
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, stringFromBuffer, Encoding.UTF8);
                                        nullHandling += stringFromBuffer.Length.ToString();
                                        break;
                                    case DataType.DT_R4:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetSingle(columnToProcessID));
                                        break;
                                    case DataType.DT_R8:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetDouble(columnToProcessID));
                                        break;
                                    case DataType.DT_UI1:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetByte(columnToProcessID));
                                        break;
                                    case DataType.DT_UI2:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetUInt16(columnToProcessID));
                                        break;
                                    case DataType.DT_UI4:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetUInt32(columnToProcessID));
                                        break;
                                    case DataType.DT_UI8:
                                        Utility.Append(ref inputByteBuffer, ref bufferUsed, buffer.GetUInt64(columnToProcessID));
                                        break;
                                    case DataType.DT_EMPTY:
                                    case DataType.DT_NULL:
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                nullHandling += "Y";
                            }
                        }

                        Utility.Append(ref inputByteBuffer, ref bufferUsed, nullHandling, Encoding.UTF8);

                        var sha1HashDual = new SHA1CryptoServiceProvider();
                        var fhash = sha1HashDual.ComputeHash(inputByteBuffer);
                        var reverseByteBuffer = inputByteBuffer.Reverse().ToArray();
                        var rhash = sha1HashDual.ComputeHash(reverseByteBuffer);
                        
                        var hash1 = BitConverter.ToString(fhash).Replace("-", ""); // + "~" + BitConverter.ToString(rhash);
                        var hash2 = BitConverter.ToString(rhash).Replace("-", "");
                        buffer.SetString(outputColumnInfos[0].bufferColumnIndex, hash1);
                        buffer.SetString(outputColumnInfos[1].bufferColumnIndex, hash2);
                        //buffer.SetInt16(outputColumnInfos[2].bufferColumnIndex, (Int16)(Math.Abs(BitConverter.ToInt16(fhash, 0)) % noOfPartitions));

                        /// Finished processing each of the columns in this row.
                        /// If an error occurred and the error output is configured, then the row has already been directed to the error output, if configured.
                        /// If not, then direct the row to the default output.
                        if (!isError)
                        {
                            buffer.DirectRow(defaultOutputId);
                        }
                    }
                }
            }
        }

        public static byte[] Sha1Hash(string data)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(data));

            //StringBuilder stringBuilder = new StringBuilder();
            //foreach (byte b in hash)
            //{
            //    stringBuilder.AppendFormat("{0:x2}", b);
            //}
            return hash; //stringBuilder.ToString();
        }
        #endregion

        /// <summary>
        /// Called by the data flow to retrieve description of the error code provided to DirectErrorRow during ProcessInput.
        /// </summary>
        /// <param name="iErrorCode">The error code to retrieve the message for.</param>
        /// <returns>A string containing the error description.</returns>
        public override string DescribeRedirectedErrorCode(int iErrorCode)
        {
            return iErrorCode == INVALID_CHARACTER_INDEX ? InvalidCharacterIndexmessage : "";
        }

        #endregion
    }
}
