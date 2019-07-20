using System;
using System.Collections.Generic;
using System.Linq;

namespace NumpyBase
{
    public class numpy<T>
    {
        //private data
        private Array _data;
        private int _rank;
        private int _length;
        private int[] _shape;

        //public data
        public int dim { get { return _rank; } }
        public int[] Shape
        {
            set
            {
                for (int i = 0; i < _rank; i++)
                    _shape[i] = value.GetLength(i);
            }
            get { return _shape; }
        }
        public int Length { get { return _data.Length; } }

        //constructor
        public numpy()
        {
            _data = new Array[0];
            _rank = 0;
            _length = 0;
            _shape = new int[0];
        }
        public numpy(T dt)
        {
            // if the parameter is a single integer,np[parameter] could be used
            // We don't have to change the integer parameter to string index
            _data = new T[1];
            _data.SetValue(dt, 0);
            _rank = 1;
            _length = 1;
            _shape = new int[1] { 1 };
        }
        public numpy(Array arr)
        {
            _data = arr;// shallow copy
            _rank = arr.Rank;
            _length = arr.Length;
            _shape = new int[_rank];
            for (int i = 0; i < _rank; i++)
                _shape[i] = arr.GetLength(i);
            // deep copy
            //_data = Array.CreateInstance(typeof(T), _shape);
            //Array.Copy(arr, _data, arr.Length);
        }
        public numpy(numpy<T> np)
        {
            _data = np._data;
            _rank = np.dim;
            _length = np.Length;
            _shape = np.Shape;
        }

        //private function for invoke
        private int _num_dealer(int num, int len)
        {
            // process nagative number for slice index
            // input: index number,array length
            // return: index number after dealing nagative situation   
            int _num = num >= 0 ? num : num % len + len;
            if (_num > len)
                throw new IndexOutOfRangeException("Axis is out of bounds for array of dimension");
            return _num;
        }
        private int[,] _index_dealer(string index)
        {
            // This function transfers string index to parameter 2-dims array.
            // input: string index of numpy object
            // return: Array of index,include start and end index for each part,
            //         array[a,b], a infers numbers of dimensions(or rank),b
            //         infers start/end index of slice you are dealing with.
            // ------
            // EXAMPLE:(writen in c# but presented in python IDE)
            // >>> 
            // >>> index_dealer("1:2") // int[4]---shape=[4]
            // >>> { {1,2} }
            // >>> index_dealer("0:-1")// int[4]---shape=[4]
            // >>> { {0,3} }
            // >>> index_dealer(":,:,2") // int[3,3,3]---shape=[3,3,3]
            // >>> { {0,3},{0,3},{2,3} }
            // >>> index_dealer("1:2,0:-1,:") // int [4,5,4]---shape=[4,5,4]
            // >>> { {1,2},{0,4},{0,4} }
            // >>> index_dealer("...,2") // int[3,3,3] ---shape =[3,3,3]
            // >>> { {0,3},{0,3},{2,3} }
            // ------
            // ALL INDICES ARE ZERO-BASED
            var tokens = index.Split(',');
            var idx_list = new int[_rank, 2];

            if (_rank == 0)
                throw new ArgumentNullException("Empty array couldn't be indexed");
            if (tokens.Length > _rank)
                throw new ArgumentException("Index must match the dimensons of Array");
            else if (tokens.Length < _rank)//contains ellipsis ('...')
            {
                // Locate the start index of "..." in the tokens array
                var tmp1 = Array.IndexOf(tokens, "...");
                var tmp2 = Array.LastIndexOf(tokens, "...");
                // Sanity checks
                if (tmp1 == -1)//"..." not found
                    throw new ArgumentNullException("Index must match the dimensons of Array");//TODO....
                if (tmp1 != tmp2)
                    throw new ArgumentNullException("An index can only have a single ellipsis ('...')");

                var tmp_len = tokens.Length;
                // before "..."
                if (tmp1 != 0)// "..." not located at the start
                {
                    for (int i = 0; i < tmp1; i++)
                    {
                        // one of the slice is integer case,eg arr[:,:,3]
                        if (!tokens[i].Contains(':'))
                        {
                            try
                            {
                                if (int.Parse(tokens[i]) <= _data.GetLength(i))//equal to arr[:,:,3:4]
                                {
                                    idx_list[i, 0] = _num_dealer(int.Parse(tokens[i]), _shape[i]);
                                    idx_list[i, 1] = _num_dealer(idx_list[i, 0] + 1, _shape[i]);
                                }
                            }
                            catch (IndexOutOfRangeException e)
                            {
                                Console.WriteLine("Message:{0}", e.Message);
                            }
                        }
                        // slice like arr[a:,:d,e:f]
                        else
                        {
                            var tmp = tokens[i].Split(':');
                            if (tmp.Length > 2)
                                throw new ArgumentException("Too many indices ':' for an array.");
                            else
                            {
                                //start slice index
                                if (tmp[0] == "")
                                    idx_list[i, 0] = 0;
                                else
                                    idx_list[i, 0] = int.Parse(tmp[0]);
                                //end slice index
                                if (tmp[1] == "")
                                    idx_list[i, 1] = _shape[i];
                                else
                                    idx_list[i, 1] = int.Parse(tmp[1]);
                            }
                        }
                    }
                }
                // the "..." part
                for (var i = tmp1; i < tmp1 + _rank - tmp_len + 1; i++) // CHECK THIS
                {
                    // "..." means recognize all remain dims to ":"
                    idx_list[i, 0] = 0;
                    idx_list[i, 1] = _shape[i];
                }
                // after "..."
                if (tmp1 != tmp_len - 1) //"..." not located at the end
                {
                    for (int i = tmp1 + _rank - tmp_len + 1; i < _rank; i++)
                    {
                        var _i = i - _rank + tmp_len; //offset the distance
                        // one of the slice is integer case,eg arr[:,:,3]
                        if (!tokens[_i].Contains(':'))
                        {
                            try
                            {
                                if (int.Parse(tokens[_i]) <= _data.GetLength(i))//equal to arr[:,:,3:4]
                                {
                                    idx_list[i, 0] = _num_dealer(int.Parse(tokens[_i]), _shape[i]);
                                    idx_list[i, 1] = _num_dealer(idx_list[i, 0] + 1, _shape[i]);
                                }
                            }
                            catch (IndexOutOfRangeException e)
                            {
                                Console.WriteLine("Message:{0}", e.Message);
                            }
                        }
                        // slice like arr[a:,:d,e:f]
                        else
                        {
                            var tmp = tokens[_i].Split(':');
                            if (tmp.Length > 2)
                                throw new ArgumentException("Too many indices ':' for an array.");
                            else
                            {
                                //start slice index
                                if (tmp[0] == "")
                                    idx_list[i, 0] = 0;
                                else
                                    idx_list[i, 0] = int.Parse(tmp[0]);
                                //end slice index
                                if (tmp[1] == "")
                                    idx_list[i, 1] = _shape[i];
                                else
                                    idx_list[i, 1] = int.Parse(tmp[1]);
                            }
                        }
                    }
                }
            }
            // "Regular" case: doesn't include ellipsis("...")
            else
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    // one of the slice is integer case,eg arr[:,:,3]
                    if (!tokens[i].Contains(':'))
                    {
                        if (tokens[i].Contains("..."))
                        {
                            // "..." means recognize all remain dims to ":"
                            idx_list[i, 0] = 0;
                            idx_list[i, 1] = _shape[i];
                        }
                        else
                        {
                            try
                            {
                                if (int.Parse(tokens[i]) <= _data.GetLength(i))//equal to arr[:,:,3:4]
                                {
                                    idx_list[i, 0] = _num_dealer(int.Parse(tokens[i]), _shape[i]);
                                    idx_list[i, 1] = _num_dealer(idx_list[i, 0] + 1, _shape[i]);
                                }
                            }
                            catch (IndexOutOfRangeException e)
                            {
                                Console.WriteLine("Message:{0}", e.Message);
                            }
                        }
                    }
                    // slice like arr[a:,:d,e:f]
                    else
                    {
                        var tmp = tokens[i].Split(':');
                        if (tmp.Length > 2)
                            throw new ArgumentException("Too many indices ':' for an array.");
                        else
                        {
                            //start slice index
                            if (tmp[0] == "")
                                idx_list[i, 0] = 0;
                            else
                                idx_list[i, 0] = int.Parse(tmp[0]);
                            //end slice index
                            if (tmp[1] == "")
                                idx_list[i, 1] = _shape[i];
                            else
                                idx_list[i, 1] = int.Parse(tmp[1]);
                        }
                    }
                }
            }

            // processing the nagative situation
            for (int i = 0; i < _rank; i++)
            {
                var dl = Shape[i];//dimension length for each part in shape array
                var tmpa = _num_dealer(idx_list[i, 0], dl);//process start number
                var tmpb = _num_dealer(idx_list[i, 1], dl);//process end number
                if (tmpa > tmpb)//sanity checks,start should less than end
                    throw new IndexOutOfRangeException();
                else
                {
                    idx_list[i, 0] = tmpa;//start
                    idx_list[i, 1] = tmpb;//end
                }
            }
            return idx_list;
        }
        private int _len_cal(List<int[]> L)
        {
            // this is used for calculating the length of the index List
            int len = 1;
            for (var i = 0; i < _rank; i++)//for each dim
            {
                var tmp = L[i][1] - L[i][0];
                len *= tmp;
            }
            return len;
        }
        private int _len_cal(int[,] L)
        {
            // this is used for calculating the length of the index array
            int len = 1;
            for (var i = 0; i < _rank; i++)//for each dim
            {
                var tmp = L[i, 1] - L[i, 0];
                len *= tmp;
            }
            return len;
        }
        private int _len_cal(int[] shape)
        {
            // return the length of input shape array
            var len = 1;
            for (var i = 0; i < shape.Length; i++)
                len *= shape[i];
            return len;
        }
        private int[] _start_end(int[,] L)
        {
            // this function return the list of length of each dimension,
            // or the shape[] array of the index, in the same word
            // it stores the length like:[len(dim1),len(dim2),...]
            // this is used for the index processing.
            var len_list = new int[_rank];
            for (var i = 0; i < _rank; i++)
                len_list[i] = L[i, 1] - L[i, 0];
            return len_list;
        }

        //indexer
        public numpy<T> this[int index]
        {
            // if the parameter is a single integer,np[parameter] could be used
            // We don't have to transfer the integer parameter to string index
            set
            {
                var _index = _num_dealer(index, _shape[0]);
                if (_rank < 1)
                    throw new IndexOutOfRangeException();
                // one-dimension case
                else if (_rank == 1)
                    _data.SetValue(value._data.GetValue(0), index);
                // multi-dimension case
                else
                {
                    var _paramList = new int[_rank, 2];
                    _paramList[0, 0] = _index; // A[a,:,:] is equal to A[a:a+1,:,:]
                    _paramList[0, 1] = _index + 1;
                    for (var i = 1; i < _rank; i++) // A[a,:,:] is equal to A[a:a+1,0:dim_length1,0:dim_length2]...
                    {
                        _paramList[i, 0] = 0;
                        _paramList[i, 1] = Shape[i];
                    }
                    this[_paramList] = value;// not checked 
                }
            }
            get
            {
                var _index = _num_dealer(index, _shape[0]);
                if (_rank < 1)
                    throw new IndexOutOfRangeException();
                // one-dimension case
                else if (_rank == 1)
                {
                    return new numpy<T>((T)_data.GetValue(index));
                    //.....may have bug here, should we return type T? Solved, return s single numpy object
                }
                // multi-dimension case
                else
                {
                    var _paramList = new int[_rank, 2];
                    _paramList[0, 0] = _index; // A[a,:,:] is equal to A[a:a+1,:,:]
                    _paramList[0, 1] = _index + 1;
                    for (var i = 1; i < _rank; i++) // A[a,:,:] is equal to A[a:a+1,0:dim_length1,0:dim_length2]...
                    {
                        _paramList[i, 0] = 0;
                        _paramList[i, 1] = Shape[i];
                    }
                    return this[_paramList];// not checked 
                }
            }
        }
        public T this[params int[] indices]
        {
            // this indexer emulate the array indexer
            // input: integer index array
            // --------
            // EXAMPLE(written in c# but presented in python IDE)
            // >>> var a3 = new int[3,2] { { 1, 2 }, { 3, 4 }, { 5, 5 } };
            // >>> var A3 = new numpy<int>(a3);
            // >>> A3[2,1]
            // >>> 5
            // >>> A3[2,1] = 100
            // >>> A3
            // >>> { { 1, 2 }, { 3, 4 }, { 5, 100 } }
            // --------
            set
            {
                // sanity check
                if (indices.Length != _rank)
                    throw new ArgumentException("Index should match the rank of array.");
                else
                {
                    //eg. np[2,2,2] = value, 
                    _data.SetValue(value, indices);
                }
            }
            get
            {
                var _indices = new int[indices.Length];
                for (var i = 0; i < indices.Length; i++)
                    _indices[i] = _num_dealer(indices[i], _shape[i]);
                T data; // return data
                // sanity check
                if (indices.Length != _rank)
                    throw new ArgumentException("Index should match the rank of array.");
                else
                    data = (T)_data.GetValue(_indices);
                return data;
            }
        }
        public numpy<T> this[int[,] dims]
        {
            // process multi-parameters array slices and set its indexer
            // input index: parameters array like [start,end]
            // -------
            // EXAMPLE-One(written in c# but presented in python IDE)
            // >>> var a7 = new int[,]{{0,3},{1,2},{1,3}};
            // >>> var a8 = new int[,,] { { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } }, 
            // >>>                        { { 9, 10, 11 }, { 12, 13, 14 }, { 15, 16, 17 } }, 
            // >>>                        { { 18, 19, 20 }, { 21, 22, 23 }, { 24, 25, 26 } } };
            // >>> var a9 = new int[,,] { { { 99, 0 } }, 
            // >>>                        { { 0, 0 } }, 
            // >>>                        { { 0, 0 } } };
            // >>> var A8 = new numpy<int>(a8);
            // >>> var A9 = new numpy<int>(a9);   
            // >>> A8[a7] = A9
            // >>> A8
            // >>> { { { 0, 1, 2 }, { 3, 99, 0 }, { 6, 7, 8 } }, 
            // >>> { { 9, 10, 11 }, { 12, 0, 0 }, { 15, 16, 17 } }, 
            // >>> { { 18, 19, 20 }, { 21, 0, 0 }, { 24, 25, 26 } } }
            // >>> A8[a7]
            // >>> { { { 99, 0 } }, 
            // >>>   { { 0, 0 } }, 
            // >>>   { { 0, 0 } } };
            // -------
            set
            {
                // Sanity check
                if (dims.GetLength(0) != value.dim)// do not support broadcasting
                    throw new ArgumentException(" Value should have same shape of index");
                var _dims = dims.GetLength(0);
                if (_dims != dim)
                    throw new ArgumentException(" Index should match the dimensions");
                else
                {
                    // iter[] is an integer array stores length of each dims and used for indexing through carrying places
                    // such as [len(dim1),len(dim2),...]
                    var iter = _start_end(dims);
                    // initialize iter elements to zero
                    for (var i = 0; i < iter.Length; i++)
                        iter[i] = 0;
                    // this variable stores the total times of looping 
                    var _totaltimes = _len_cal(dims);
                    var count = 0; // time of each looping 
                    var _start = new int[iter.Length];// this stores the start index of each dimensions
                    for (var i = 0; i < _start.Length; i++)
                        _start[i] = dims[i, 0];
                    var stop = _start_end(dims)[_rank - 1];//len(dim-n)
                    while (count < _totaltimes) // if still looping in total times
                    {
                        for (var i = 0; i < stop; i++) // looping number in one places
                        {
                            _data.SetValue(value[iter], _start); // check this - ok,2018.08.14 11:27:04
                            count++;
                            iter[iter.Length - 1]++;
                            _start[iter.Length - 1]++;
                        }
                        for (var j = iter.Length - 1; j >= 0; j--)// update iter[] value
                        {
                            if (iter[j] == _start_end(dims)[j]) // if places need to carry
                            {
                                if (j == 0)// if the last number is done
                                    break;
                                else// carry
                                {
                                    iter[j - 1]++;
                                    iter[j] = 0;
                                    _start[j - 1]++;
                                    _start[j] = dims[j, 0];
                                }
                            }
                        }
                    } // end of loop
                }
            }
            get
            {
                // Initialize the result numpy object
                var tmp_shape = _start_end(dims);
                var tmp_object = new numpy<T>(dims);
                var result = tmp_object.zeros(tmp_shape);
                // Sanity check
                var _dims = dims.GetLength(0);
                if (_dims != dim)
                    throw new ArgumentException(" Index should match the dimensions");
                else
                {
                    // iter[] is an integer array stores length of each dims
                    // such as [len(dim1),len(dim2),...]
                    var iter = _start_end(dims);
                    // initialize iter elements to zero
                    for (var i = 0; i < iter.Length; i++)
                        iter[i] = 0;
                    // this variable stores the total times of looping 
                    var _totaltimes = _len_cal(dims);
                    var count = 0; // time of each looping 
                    var _start = new int[iter.Length];// this stores the start index of each dimensions
                    for (var i = 0; i < _start.Length; i++)
                        _start[i] = dims[i, 0];
                    var stop = _start_end(dims)[_rank - 1];//len(dim-n)
                    while (count < _totaltimes)
                    {
                        for (var i = 0; i < stop; i++)
                        {
                            result._data.SetValue(this[_start], iter); //check this- ok,2018.08.14 11:31:44
                            count++;
                            iter[iter.Length - 1]++;
                            _start[iter.Length - 1]++;
                        }
                        for (var j = iter.Length - 1; j >= 0; j--)
                        {
                            if (iter[j] == _start_end(dims)[j])
                            {
                                if (j == 0)
                                    break;
                                else
                                {
                                    iter[j - 1]++;
                                    iter[j] = 0;
                                    _start[j - 1]++;
                                    _start[j] = dims[j, 0];
                                }
                            }
                        }
                    } // end of loop
                }
                // I don't understand why numpy do this.. why squeeze the last dimension if equals to 1?
                if (result.Shape[result.Shape.Length - 1] == 1)
                    result = result.squeeze(-1);
                return result;
            }
        }
        public numpy<T> this[string index]
        {
            set
            {
                var plist = _index_dealer(index);
                //var len = _len_cal(plist);
                this[plist] = value;
            }
            get
            {
                var plist = _index_dealer(index);
                //var len = _len_cal(plist);
                return this[plist];
            }
        }

        //public function for invoke
        public numpy<T> zeros(params int[] shape)
        {
            // This function return the zeros-matrix in a np object
            // input: the shape[] matrix 
            // it could be used in some initialization cases
            if (shape.Contains(0))
                return new numpy<T>();
            var arr = Array.CreateInstance(typeof(T), shape);
            var result = new numpy<T>(arr);
            return result;
        }
        public numpy<T> ones(params int[] shape)
        {
            // This function return the ones-matrix in a np object
            // input: the shape[] matrix 
            // it could be used in some initialization cases
            var arr = Array.CreateInstance(typeof(T), shape);
            var np = new numpy<T>(arr);
            var total_times = _len_cal(shape);
            if (shape.Contains(0))
                return new numpy<T>();
            var count = 0;
            var stop = shape[shape.Length - 1];
            var iter_np = new int[shape.Length];// iter array
            //traverse
            while (count < total_times)
            {
                for (int i = 0; i < stop; i++)
                {
                    np._data.SetValue(1, iter_np);
                    iter_np[shape.Length - 1]++;
                    count++;
                }
                for (int i = shape.Length - 1; i >= 0; i--)
                {
                    if (iter_np[i] == shape[i])
                    {
                        if (i == 0) break;
                        iter_np[i - 1]++;
                        iter_np[i] = 0;
                    }
                }
            }
            return np;
        }
        public numpy<T> eye(int n)
        {
            // This function return a 2-D array with ones on the diagonal and zeros elsewhere.
            // input: the length n of each dimension, should be same
            // return: a n*n diagonal matrix
            // it could be used in some initialization cases

            var arr = new int[n, n];// a 2-d array
            for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    arr[i, j] = i == j ? 1 : 0;
            var np = new numpy<T>(arr);

            return np;
        }
        public numpy<T> eye(int n, int m)
        {
            // This function return a 2-D array with ones on the diagonal and zeros elsewhere.
            // input: the  length n, m of each dimension
            // return: a n*m diagonal matrix 
            // it could be used in some initialization cases

            var arr = new int[n, m];// a 2-d array
            for (var i = 0; i < n; i++)
                for (var j = 0; j < m; j++)
                    arr[i, j] = i == j ? 1 : 0;
            var np = new numpy<T>(arr);
            return np;
        }
        public numpy<T> nums(int[] shape, T num)
        {
            // This function return the numbers-matrix in a np object
            // input: the wonting shape and a single number
            // return: a np object with a input shape, each value is 
            //         the input number  
            // it could be used in some initialization cases
            var arr = Array.CreateInstance(typeof(T), shape);
            var np = new numpy<T>(arr);
            var total_times = _len_cal(shape);
            if (shape.Contains(0))
                return new numpy<T>();
            var count = 0;
            var stop = shape[shape.Length - 1];
            var iter_np = new int[shape.Length];// iter array
            //traverse
            while (count < total_times)
            {
                for (int i = 0; i < stop; i++)
                {
                    np._data.SetValue(num, iter_np);
                    iter_np[shape.Length - 1]++;
                    count++;
                }
                for (int i = shape.Length - 1; i >= 0; i--)
                {
                    if (iter_np[i] == shape[i])
                    {
                        if (i == 0) break;
                        iter_np[i - 1]++;
                        iter_np[i] = 0;
                    }
                }
            }
            return np;
        }
        public override string ToString()
        {
            // this function visualize the data array in a python's view 
            string str = "";
            var iter = new int[_rank];
            var count = _rank;
            while (true)
            {
                for (var i = 0; i < count; i++)
                {
                    str += "[";
                }
                count = 0;
                T dt = (T)_data.GetValue(iter);
                str += dt.ToString();
                // increment
                iter[_rank - 1]++;
                for (var i = _rank - 1; i > 0; i--)
                {
                    if (iter[i] == _shape[i])
                    {
                        iter[i] = 0;
                        iter[i - 1]++;
                        str += "]";
                        count++;
                    }
                }
                if (iter[0] == _shape[0])
                {
                    str += "]";
                    break;
                }
                str += ",";
            }
            return str;
        }
        public Array ToArray()
        {
            // this function transfer numpy object to array
            // it could be used in further calculation based on Array
            // with other API only for Array object.
            // deep copy
            var data = Array.CreateInstance(typeof(T), _shape);
            Array.Copy(_data, data, _data.Length);
            return data;
        }
        public numpy<T> reshape(params int[] _reshape)
        {
            // this function reshape the matrix with respect to the input params
            // input: the reshape parameters array
            // return: a newly reshaped numpy object
            var total_times = _len_cal(_reshape);  //loop times          
            if (total_times != _length)
                throw new ArgumentException("Cannot reshape array due to mismatching length");
            if (_reshape.Contains(0))//zero-dim case
                return new numpy<T>();

            var np = new numpy<T>().zeros(_reshape);
            var count = 0;
            var stop = _reshape[_reshape.Length - 1];
            var iter_np = new int[_reshape.Length];// new iter array for reshaped array index
            var iter_this = new int[_shape.Length];// iter array for this(object)'s array index
            // Traverse
            while (count < total_times)
            {
                for (int i = 0; i < stop; i++)
                {
                    // start from one place, emulating mathemetical addition calculation
                    np._data.SetValue(this[iter_this], iter_np);
                    iter_np[_reshape.Length - 1]++;
                    iter_this[_shape.Length - 1]++;
                    count++;

                    if (iter_this[_shape.Length - 1] == _shape[_shape.Length - 1])
                    {
                        if (_shape.Length == 1) break;
                        iter_this[_shape.Length - 2]++;
                        iter_this[_shape.Length - 1] = 0;
                    }
                }
                for (int i = _reshape.Length - 1; i >= 0; i--)
                {
                    // update the reshape array index
                    if (iter_np[i] == _reshape[i])
                    {
                        if (i == 0) break;
                        iter_np[i - 1]++;
                        iter_np[i] = 0;
                    }
                }
                for (int i = _shape.Length - 1; i >= 0; i--)
                {
                    // update this._shape array index
                    if (iter_this[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter_this[i - 1]++;
                        iter_this[i] = 0;
                    }
                }
            }
            return np;
        }
        public numpy<int> arange(int r)
        {
            // this is a function resemble numpy arange function
            // input: single integer number r
            // return: enumerable 1-D array numpy object in range(0,r)
            // EXAMPLE---
            // >>> var a = new numpy<int>().arange(3)
            // >>> a 
            // >>> [0,1,2]
            // >>> var b = new numpy<int>().arange(9).reshape(3,3)
            // >>> b
            // >>> [[0, 1, 2],
            // ...  [3, 4, 5],
            // ...  [6, 7, 8]]
            var arr = new int[r];
            foreach (var i in Enumerable.Range(0, r))
                arr[i] = i;
            var range = new numpy<int>(arr);
            return range;
        }
        public numpy<double> arange(double r)
        {
            // this is a function resemble numpy arange function
            // input: single double number r
            // return: enumerable 1-D array numpy object in range(0,r)
            // EXAMPLE---
            // >>> var a = new numpy<double>().arange(3.0)
            // >>> a 
            // >>> [0.0,1.0,2.0]
            // >>> var b = new numpy<double>().arange(9.0).reshape(3,3)
            // >>> b
            // >>> [[0.0, 1.0, 2.0],
            // ...  [3.0, 4.0, 5.0],
            // ...  [6.0, 7.0, 8.0]]
            var arr = new double[(int)r];
            foreach (var i in Enumerable.Range(0, (int)r))
                arr[i] = (double)i;
            var range = new numpy<double>(arr);
            return range;
        }
        public numpy<int> arange(int start, int stop)
        {
            // this is a function resemble numpy arange function
            // input: integer start number, integer end number
            // return: enumerable 1-D array numpy object in range(start,end)
            // EXAMPLES---
            // >>> var a = new numpy<int>().arange(2,6)
            // >>> a 
            // >>> [2,3,4,5]

            var r = stop - start;
            if (r <= 0)
                throw new ArgumentException("start index must be greater than end index.");
            var arr = new int[r];
            foreach (var i in Enumerable.Range(0, r))
                arr[i] = i + start;
            var range = new numpy<int>(arr);
            return range;
        }
        public numpy<double> arange(double start, double stop)
        {
            // this is a function resemble numpy arange function
            // input: double start number, double end number
            // return: enumerable 1-D array numpy object in range(start,end)
            var r = stop - start;
            if (r <= 0)
                throw new ArgumentException("start index must be greater than end index.");
            var arr = new double[(int)r];
            foreach (var i in Enumerable.Range(0, (int)r))
                arr[i] = (double)i + start;
            var range = new numpy<double>(arr);
            return range;
        }
        public dynamic sum()
        {
            // return _data's sum, which is a scalar
            dynamic result = 0;
            foreach (var i in _data)
                result += (dynamic)i;
            return result;
        }
        public numpy<T> sum(int axis)
        {
            // this function sum the data by provided axis
            // input: integer number axis
            // return: a new sum-up numpy array object

            var _reshape = new int[_shape.Length - 1];
            // deal with nagative case
            axis = _num_dealer(axis, _shape.Length);
            if (axis >= _shape.Length)
                throw new IndexOutOfRangeException("Axis is out of bounds for array of dimension");
            // initialize the result's shape
            for (int i = 0; i < _shape.Length; i++)
            {
                if (i < axis)
                    _reshape[i] = _shape[i];
                else if (i == axis)
                    continue;
                else
                    _reshape[i - 1] = _shape[i];
            }

            var total_times = _length;  //loop times
            var np = new numpy<T>().zeros(_reshape);
            var count = 0;
            var stop = _shape[axis];
            var iter_np = new int[_reshape.Length];// new iter array for sum-up result array index
            var iter_this = new int[_shape.Length];// iter array for this(object)'s array index
            // Traverse
            while (count < total_times)
            {
                var sum = default(T);
                for (int i = 0; i < stop; i++)
                {
                    sum += (dynamic)this._data.GetValue(iter_this);
                    iter_this[axis]++;
                    count++;

                    if (iter_this[axis] == _shape[axis])
                    {
                        if (axis == _shape.Length - 1)
                        {
                            if (axis == 0) break;
                            iter_this[axis - 1]++;
                            iter_this[axis] = 0;
                        }
                        else
                        {
                            iter_this[_shape.Length - 1]++;
                            iter_this[axis] = 0;
                        }
                    }
                }
                np._data.SetValue(sum, iter_np);
                iter_np[_reshape.Length - 1]++;
                for (int i = _reshape.Length - 1; i >= 0; i--)
                {
                    // update the reshape array index
                    if (iter_np[i] == _reshape[i])
                    {
                        if (i == 0) break;
                        iter_np[i - 1]++;
                        iter_np[i] = 0;
                    }
                }
                for (int i = _shape.Length - 1; i >= 0; i--)
                {
                    // update this._shape array index
                    if (iter_this[i] == _shape[i])
                    {
                        if (i == 0) break;
                        if (i - 1 != axis)
                            iter_this[i - 1]++;
                        else
                            if (i - 2 >= 0)
                                iter_this[i - 2]++;

                        if (i != axis)
                            iter_this[i] = 0;
                    }
                }
            }//end of loop
            return np;
        }
        public decimal decimal_sum()
        {
            // return decimal type data's sum, which is a scalar
            decimal result = 0;
            foreach (var i in _data)
                result += (decimal)i;
            return result;
        }
        public numpy<T> transpose(params int[] seq)
        {
            // this function return a newly transposed matrix by given sequence
            // input: sequence array, indicating the transpose sequence
            // EXAMPLE one ------(presented in a python IDE way for easily understanding)
            // >>> int[] seq_old = new int[]{0,1,2}; 
            // >>> array_old = new numpy<int>().arange(8).reshape(2,2,2)
            // >>> array_old
            // >>> [[[0,1],[2,3]],[[4,5],[6,7]]]
            // this implys that array_old have 3 dims, presented in a primitive sequence: {0,1,2}
            // we could set new sequence to transpose it, for example: {2,0,1}
            // >>> array_new = array_old.transpose(2,0,1)
            // >>> array_new 
            // >>> [[[0,2],[4,6]],[[1,3],[5,7]]]
            // EXAMPLE two
            // a more easy understand perspective: 2-D matrix 
            // >>> arr = new numpy<int>().arange(6).reshape(3,2) // shape:(3,2) in sequence: [0,1]
            // >>> arr
            // >>> [[0,1],
            // ...  [2,3],
            // ...  [4,5]]
            // >>> arr.transpose(1,0) // new sequence: [1,0]
            // >>> [[0,2,4],
            // ...  [1,3,5]]
            // If we want to reverse the initial sequence of the shape[], we could use np.t(), for convenience.

            // Sanity check
            if (seq.Length != _rank)
                throw new ArgumentException("Axes don't match array");
            foreach (var i in seq)
                if (i > _rank - 1 || i < 0)
                    throw new IndexOutOfRangeException("Axis is out of bounds for array of dimension");
            if (Enumerable.Range(0, seq.Length).Sum() != seq.Sum())
                throw new ArgumentException("Repeated axis in transpose");

            var total_times = _length;//loop times
            // _shape.Length == _reshape.Length == _rank == seq.Length
            // the reshape array is the transposed array's shape
            // which may not have the same sequence as this.shape
            var _reshape = new int[_shape.Length];
            // initialize the reshape array, ensure the iterative sequence
            for (int i = _shape.Length - 1; i >= 0; i--)
                _reshape[Array.IndexOf(seq, i)] = _shape[i];

            var np = new numpy<T>().zeros(_reshape);
            var count = 0;
            var stop = _reshape[Array.IndexOf(seq, _reshape.Length - 1)];
            var iter_np = new int[_reshape.Length];// new iter array for transposed array index
            var iter_this = new int[_shape.Length];// iter array for this(object)'s array index
            // Traverse
            while (count < total_times)
            {
                for (int i = 0; i < stop; i++)
                {
                    np._data.SetValue(this[iter_this], iter_np);
                    iter_np[Array.IndexOf(seq, _reshape.Length - 1)]++;
                    iter_this[_shape.Length - 1]++;
                    count++;

                    if (iter_this[_shape.Length - 1] == _shape[_shape.Length - 1])
                    {
                        if (_shape.Length == 1) break;
                        iter_this[_shape.Length - 2]++;
                        iter_this[_shape.Length - 1] = 0;
                    }
                }
                for (int i = _reshape.Length - 1; i >= 0; i--)
                {
                    // start from the greatest number of the reshape array 
                    if (iter_np[Array.IndexOf(seq, i)]
                        == _reshape[Array.IndexOf(seq, i)])// carry places
                    {
                        // if reach the smallest number, 0, break
                        if (i == 0) break;
                        // else, find the second greatest number, carry place to it
                        iter_np[Array.IndexOf(seq, i - 1)]++;
                        // and reset it's own place to zero
                        iter_np[Array.IndexOf(seq, i)] = 0;
                    }
                }
                for (int i = _shape.Length - 1; i >= 0; i--)
                {
                    if (iter_this[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter_this[i - 1]++;
                        iter_this[i] = 0;
                    }
                }
            } // end of loop
            return np;
        }
        public numpy<T> t()
        {
            // this function return a new transposed array of itself 
            // it resemble the numpy function(".T")
            // _shape.Length == _reshape.Length == _rank == seq.Length
            var _reshape = new int[_shape.Length];
            // initialize the reshape array, the iterative sequence is descending
            for (int i = 0; i < _shape.Length; i++)
                _reshape[_shape.Length - i - 1] = i;
            return this.transpose(_reshape);
        }
        public numpy<T> Square()
        {
            // this fucntion return the squared value of each data
            // which is a new numpy object

            var data = Array.CreateInstance(typeof(T), _shape);
            Array.Copy(_data, data, _data.Length);

            var iter = new int[_shape.Length];
            var count = 0;
            var total_time = _len_cal(_shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < _shape[_shape.Length - 1]; i++)
                {
                    dynamic _a = _data.GetValue(iter);

                    data.SetValue(_a * _a, iter);
                    iter[_shape.Length - 1]++;
                    count++;
                }
                for (var i = _shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public numpy<T> power(double num)
        {
            // this function return a new copy of powered value of each data
            // input: the power number

            var data = Array.CreateInstance(typeof(T), _shape);
            Array.Copy(_data, data, _data.Length);

            var iter = new int[_shape.Length];
            var count = 0;
            var total_time = _len_cal(_shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < _shape[_shape.Length - 1]; i++)
                {
                    double _a = (double)_data.GetValue(iter);
                    double p = Math.Pow(_a, num);

                    data.SetValue((object)p, iter);
                    iter[_shape.Length - 1]++;
                    count++;
                }
                for (var i = _shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public numpy<T> exp()
        {
            // this fucntion return a new copy of the exponential value of each data

            var data = Array.CreateInstance(typeof(T), _shape);
            Array.Copy(_data, data, _data.Length);

            var iter = new int[_shape.Length];
            var count = 0;
            var total_time = _len_cal(_shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < _shape[_shape.Length - 1]; i++)
                {
                    double _a = (double)_data.GetValue(iter);
                    double _exp = Math.Exp(_a);
                    data.SetValue((object)_exp, iter);
                    iter[_shape.Length - 1]++;
                    count++;
                }
                for (var i = _shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public numpy<T> log(double _newbase = Math.E)
        {
            // this fucntion return a new copy log value of each data
            // input: (optional) the base number for the log function

            var data = Array.CreateInstance(typeof(T), _shape);
            Array.Copy(_data, data, _data.Length);

            var iter = new int[_shape.Length];
            var count = 0;
            var total_time = _len_cal(_shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < _shape[_shape.Length - 1]; i++)
                {
                    double _a = (double)_data.GetValue(iter);
                    double _log = Math.Log(_a, _newbase);
                    data.SetValue((object)_log, iter);
                    iter[_shape.Length - 1]++;
                    count++;
                }
                for (var i = _shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == _shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public numpy<T> concatenate(numpy<T> a, numpy<T> b, int axis = 0)
        {
            // this function joins a sequence of arrays along an existing axis.

            // Sanity check
            if (a.dim != b.dim)
                throw new RankException("Input array's dimension should be equal.");
            axis = _num_dealer(axis, a.dim);// deal with nagative cases
            if (axis >= a.dim)
                throw new IndexOutOfRangeException("Axis is out of bounds for array of dimension");
            var len = a.Shape[axis];// store a.shape[axis] for iteration, index convert from a to b
            for (int i = 0; i < a.dim; i++)
                if (a.Shape[i] != b.Shape[i])
                    if (i != axis)
                        throw new RankException("all the input array dimensions except for the " +
                                                "concatenation axis must match exactly");
            // Create new shape for concatenate array
            var shape = new int[a.dim];
            for (var i = 0; i < a.dim; i++)
            {
                if (i == axis)
                    shape[i] = a.Shape[i] + b.Shape[i];
                else
                    shape[i] = a.Shape[i];
            }
            var c = new numpy<T>().zeros(shape);
            // prepare for travese
            var total_times = c._length;  //loop times
            var count = 0;
            var stop = shape[axis];
            var iter_c = new int[shape.Length];// new iter array for concatanated array index
            var iter_a = new int[a.Shape.Length];// iter array for a's array index
            var iter_b = new int[b.Shape.Length];// iter array for b's array index
            // Traverse
            while (count < total_times)
            {
                for (int i = 0; i < stop; i++)
                {
                    if (i < len)
                    {
                        var dt = a._data.GetValue(iter_a);
                        c._data.SetValue((object)dt, iter_c);
                        iter_a[axis]++;
                        iter_c[axis]++;
                        count++;
                    }
                    else
                    {
                        var dt = b._data.GetValue(iter_b);
                        c._data.SetValue((object)dt, iter_c);
                        iter_b[axis]++;
                        iter_c[axis]++;
                        count++;
                    }
                    // c
                    if (iter_c[axis] == c.Shape[axis])
                    {
                        if (axis == c.Shape.Length - 1)
                        {
                            if (axis == 0) break;
                            iter_c[axis - 1]++;
                            iter_c[axis] = 0;
                        }
                        else
                        {
                            iter_c[c.Shape.Length - 1]++;
                            iter_c[axis] = 0;
                        }
                    }
                    // a
                    if (iter_a[axis] == a.Shape[axis])
                    {
                        if (axis == a.Shape.Length - 1)
                        {
                            //a has been traversed, but don't break at this point, since b still untraversed
                            if (axis == 0) iter_a[axis] = 0;
                            else
                            {
                                iter_a[axis - 1]++;
                                iter_a[axis] = 0;
                            }
                        }
                        else
                        {
                            iter_a[a.Shape.Length - 1]++;
                            iter_a[axis] = 0;
                        }
                    }
                    // b
                    if (iter_b[axis] == b.Shape[axis])
                    {
                        if (axis == b.Shape.Length - 1)
                        {
                            if (axis == 0) iter_b[axis] = 0;//if break, make sure it break simultaneously with c's loop
                            else
                            {
                                iter_b[axis - 1]++;
                                iter_b[axis] = 0;
                            }
                        }
                        else
                        {
                            iter_b[b.Shape.Length - 1]++;
                            iter_b[axis] = 0;
                        }
                    }
                }
                for (int i = a.Shape.Length - 1; i >= 0; i--)
                {
                    // update this._shape array index
                    if (iter_a[i] == a.Shape[i])
                    {
                        if (i == 0) break;
                        if (i - 1 != axis)
                            iter_a[i - 1]++;
                        else
                            if (i - 2 >= 0)
                                iter_a[i - 2]++;
                        if (i != axis)
                            iter_a[i] = 0;
                    }
                }
                for (int i = b.Shape.Length - 1; i >= 0; i--)
                {
                    // update this._shape array index
                    if (iter_b[i] == b.Shape[i])
                    {
                        if (i == 0) break;
                        if (i - 1 != axis)
                            iter_b[i - 1]++;
                        else
                            if (i - 2 >= 0)
                                iter_b[i - 2]++;

                        if (i != axis)
                            iter_b[i] = 0;
                    }
                }
                for (int i = shape.Length - 1; i >= 0; i--)
                {
                    // update this._shape array index
                    if (iter_c[i] == shape[i])
                    {
                        if (i == 0) break;
                        if (i - 1 != axis)
                            iter_c[i - 1]++;
                        else
                            if (i - 2 >= 0)
                                iter_c[i - 2]++;

                        if (i != axis)
                            iter_c[i] = 0;
                    }
                }
            }//end of loop
            return c;
        }
        public numpy<T> concatenate(int axis = 0, params numpy<T>[] arr)
        {
            // this function could concatenate more than 2 array.
            var len = arr.Length;
            if (len < 2)
                throw new ArgumentNullException("should input more than one array to concatenate.");
            var np = arr[0];
            for (var i = 1; i < len; i++)
                np = np.concatenate(np, arr[i], axis);

            return np;
        }
        public numpy<T> diag(numpy<T> np)
        {
            // Extract a diagonal or construct a diagonal array.
            // sanity check
            if (np.dim > 2)
                throw new RankException("Array with more than 2 dimensions couldn't" +
                                        "construct a diagonal array, input must be 1- or 2-d.");
            var result = new numpy<T>();
            if (np.dim == 2)
            {
                var len = Math.Min(np.Shape[0], np.Shape[1]);
                var arr = new T[len];
                for (var i = 0; i < len; i++)
                    arr[i] = np[i, i];
                result = new numpy<T>(arr);
            }
            if (np.dim == 1)
            {
                var arr = new T[np.Length, np.Length];
                for (var i = 0; i < np.Length; i++)
                {
                    for (var j = 0; j < np.Length; j++)
                    {
                        if (i == j) arr[i, j] = (T)np._data.GetValue(i);
                        else
                            arr[i, j] = default(T);
                    }
                }
                result = new numpy<T>(arr);
            }
            return result;
        }
        public numpy<T> diagonal(numpy<T> np)
        {
            // this function returns specified diagonals.
            // only use for lese than 3-d cases
            // if dimension is 1-d or 2-d, return diag(np)
            // if dimension is 3-d, return the diagonal according to the last axis
            // Example:
            // >>> var a = np.arange(8).reshape(2,2,2);
            // >>> a[:,:,0]
            // >>> [[0,2],
            // ...  [4,6]]
            // >>> a[:,:,1]
            // >>> [[1,3],
            // ...  [5,7]]
            // >>> np.diagonal(a)
            // >>> [[0,6],
            // ...  [1,7]]
            // TODO: extend the situations to more than 3-d
            // Reference:
            // https://docs.scipy.org/doc/numpy/reference/generated/numpy.diagonal.html

            if (np.dim > 3)
                throw new RankException("the dimension shouldn't be greater than 3");
            if (np.dim <= 2)
                return new numpy<T>().diag(np);
            else//np.dim ==3
            {
                var num = np.Shape[2];// get the last axis length
                var len = Math.Min(np.Shape[0], np.Shape[1]);
                var arr = new T[num, len];
                for (int i = 0; i < num; i++)
                    for (int j = 0; j < len; j++)
                        arr[i, j] = np[j, j, i];
                return new numpy<T>(arr);
            }
        }
        public numpy<T> squeeze()
        {
            var L = new List<int>();
            for (var i = 0; i < _rank; i++)
                if (this._shape[i] != 1)
                    L.Add(this._shape[i]);
            var arr = new int[0];
            if (L.Count == 0)//all dimensions have only one element
                arr = new int[] { 1 };
            else
                arr = L.ToArray();
            var np = this.reshape(arr);
            return np;
        }
        public numpy<T> squeeze(int axis)
        {
            axis = _num_dealer(axis, _rank);
            if (axis >= _rank)
                throw new IndexOutOfRangeException();
            var arr = new int[_rank - 1];
            for (int i = 0; i < _rank; i++)
            {
                if (i < axis)
                    arr[i] = this._shape[i];
                else if (i > axis)
                    arr[i - 1] = this._shape[i];
                else
                    if (this._shape[i] != 1)
                        throw new ArgumentException("Cannot select an axis to squeeze" +
                                                " out which has size not equal to one");
            }
            var np = this.reshape(arr);
            return np;
        }

        //operator & broadcasting        
        public static bool operator ==(numpy<T> a, numpy<T> b)
        {
            if (a._rank != b._rank)
                return false;
            if (a._length != b._length)
                return false;
            for (var i = 0; i < a._rank; i++)//a's and b's shape[] should be identical
                if (a._shape[i] != b._shape[i])
                    return false;
            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    T _a = (T)a._data.GetValue(iter);
                    T _b = (T)b._data.GetValue(iter);
                    if (!EqualityComparer<T>.Default.Equals(_a, _b))
                        return false;
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) return true;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            return true;
        }
        public static bool operator !=(numpy<T> a, numpy<T> b)
        {
            if (a._rank != b._rank)
                return true;
            if (a._length != b._length)
                return true;
            for (var i = 0; i < a._rank; i++)//a's and b's shape[] should be identical
                if (a._shape[i] != b._shape[i])
                    return true;
            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    T _a = (T)a._data.GetValue(iter);
                    T _b = (T)b._data.GetValue(iter);
                    if (!EqualityComparer<T>.Default.Equals(_a, _b))
                        return true;
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) return false;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            return false;
        }
        public static numpy<bool> operator ==(numpy<T> a, T b)
        {
            var np_c = new numpy<bool>().nums(a._shape, false);
            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    T _a = (T)a._data.GetValue(iter);
                    if (EqualityComparer<T>.Default.Equals(_a, b))
                        np_c._data.SetValue(true, iter);
                    else
                        np_c._data.SetValue(false, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            return np_c;
        }
        public static numpy<bool> operator !=(numpy<T> a, T b)
        {
            var np_c = new numpy<bool>().nums(a._shape, false);
            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    T _a = (T)a._data.GetValue(iter);
                    if (EqualityComparer<T>.Default.Equals(_a, b))
                        np_c._data.SetValue(false, iter);
                    else
                        np_c._data.SetValue(true, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            return np_c;
        }
        public static numpy<T> operator +(numpy<T> a, numpy<T> b)
        {
            a = broadcast(a, b).Item1;
            b = broadcast(a, b).Item2;
            //if (!a._shape.SequenceEqual(b._shape))
            //    throw new ArgumentException("Cannot add arrays with different shapes");
            var data = Array.CreateInstance(typeof(T), a._shape);
            Array.Copy(a._data, data, a._data.Length);

            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    dynamic _a = (T)a._data.GetValue(iter);
                    var _b = (T)b._data.GetValue(iter);
                    data.SetValue(_a + _b, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public static numpy<T> operator +(numpy<T> a, T b)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return a + np_b;
        }
        public static numpy<T> operator +(T b, numpy<T> a)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return np_b + a;
        }
        public static numpy<T> operator -(numpy<T> a, numpy<T> b)
        {
            a = broadcast(a, b).Item1;
            b = broadcast(a, b).Item2;
            //if (!a._shape.SequenceEqual(b._shape))
            //    throw new ArgumentException("Cannot subtract arrays with different shapes");
            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            var data = Array.CreateInstance(typeof(T), a._shape);
            Array.Copy(a._data, data, a._data.Length);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    dynamic _a = (T)a._data.GetValue(iter);
                    var _b = (T)b._data.GetValue(iter);
                    data.SetValue(_a - _b, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public static numpy<T> operator -(numpy<T> a)
        {
            var data = Array.CreateInstance(typeof(T), a._shape);
            Array.Copy(a._data, data, a._data.Length);

            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    dynamic _a = (T)a._data.GetValue(iter);
                    data.SetValue(-_a, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var b = new numpy<T>(data);
            return b;
        }
        public static numpy<T> operator -(numpy<T> a, T b)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return a - np_b;
        }
        public static numpy<T> operator -(T b, numpy<T> a)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return np_b - a;
        }
        public static numpy<T> operator *(numpy<T> a, numpy<T> b)
        {
            a = broadcast(a, b).Item1;
            b = broadcast(a, b).Item2;
            //if (!a._shape.SequenceEqual(b._shape))
            //    throw new ArgumentException("Cannot multiply arrays with different shapes");
            var data = Array.CreateInstance(typeof(T), a._shape);
            Array.Copy(a._data, data, a._data.Length);

            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    dynamic _a = (T)a._data.GetValue(iter);
                    var _b = (T)b._data.GetValue(iter);
                    data.SetValue(_a * _b, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public static numpy<T> operator *(numpy<T> a, T b)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return a * np_b;
        }
        public static numpy<T> operator *(T b, numpy<T> a)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return np_b * a;
        }
        public static numpy<T> operator /(numpy<T> a, numpy<T> b)
        {
            a = broadcast(a, b).Item1;
            b = broadcast(a, b).Item2;
            //if (!a._shape.SequenceEqual(b._shape))
            //    throw new ArgumentException("Cannot multiply arrays with different shapes");
            var data = Array.CreateInstance(typeof(T), a._shape);
            Array.Copy(a._data, data, a._data.Length);

            var iter = new int[a._shape.Length];
            var count = 0;
            var total_time = a._len_cal(a._shape);
            // traverse
            while (count < total_time)
            {
                for (var i = 0; i < a._shape[a._shape.Length - 1]; i++)
                {
                    dynamic _a = (T)a._data.GetValue(iter);
                    var _b = (T)b._data.GetValue(iter);
                    data.SetValue(_a / _b, iter);
                    iter[a._shape.Length - 1]++;
                    count++;
                }
                for (var i = a._shape.Length - 1; i >= 0; i--)
                {
                    if (iter[i] == a._shape[i])
                    {
                        if (i == 0) break;
                        iter[i - 1]++;
                        iter[i] = 0;
                    }
                }
            }
            var c = new numpy<T>(data);
            return c;
        }
        public static numpy<T> operator /(numpy<T> a, T b)
        {
            if ((dynamic)b == 0)
                throw new DivideByZeroException();
            var np_b = new numpy<T>().nums(a._shape, b);
            return a / np_b;
        }
        public static numpy<T> operator /(T b, numpy<T> a)
        {
            var np_b = new numpy<T>().nums(a._shape, b);
            return np_b / a;
        }
        private static numpy<T> stretch(numpy<T> np, int[] result_shape)
        {
            // stretch the array to result array's shape.

            if (np.dim != result_shape.Length)//np.shape is shorter than result's
                extend(ref np, result_shape);//extend it so that both have same dimensions
            for (var i = result_shape.Length - 1; i >= 0; i--)
            {
                var times = result_shape[i] / np.Shape[i];//record the times we should concatenate
                if (times == 1) continue;//do not need concatenate
                else
                {
                    var arr = new numpy<T>[times];//store the numpy arrays to concatenate
                    for (var j = 0; j < times; j++)
                        arr[j] = np;
                    np = np.concatenate(i, arr);
                }
            }
            return np;
        }
        private static void extend(ref numpy<T> np, int[] result_shape)
        {
            // extend array to result array's shape, so that they have same dimensions
            // the current array should have been stretched
            // that the shape should be same as last few elemnts of result shape
            // eg. shape(2,3,1) -----extend-----> (1,2,3,1), same dims as result shape(5,2,3,1)
            // shape(2,3,1) is not same as the last few elements of shape(5,2,3,2),shape(5,4,3,1)...


            var len1 = result_shape.Length;
            var len2 = np.dim;
            var arr = new int[len1];
            for (var i = 1; i <= len1; i++)//i means the total numbers of element copy to the new arr
            {
                if (i <= len2)
                    arr[len1 - i] = np.Shape[len2 - i];
                else
                    arr[len1 - i] = 1;
            }
            np = np.reshape(arr);
        }
        private static Tuple<numpy<T>, numpy<T>> broadcast(numpy<T> a, numpy<T> b)
        {
            // private function stretches each of the array to compatible shape,
            // in order to broadcast for the operation
            // Reference:
            // https://docs.scipy.org/doc/numpy/user/basics.broadcasting.html

            // check whether each shapes could be stretched to a compatible one            
            if (!a._shape.SequenceEqual(b._shape))
            {
                //obtain the greater one as base
                var shape = (a.dim > b.dim) ? new int[a.dim] : new int[b.dim];
                if (a.dim > b.dim)
                    Array.Copy(a.Shape, shape, a.Shape.Length);
                else
                    Array.Copy(b.Shape, shape, b.Shape.Length);

                var len = a.dim > b.dim ? b.Shape.Length : a.Shape.Length;//obtain the smaller one's length
                var len_a = a.dim; var len_b = b.dim;
                // create a new shape for broadcasting.
                for (var i = 1; i <= len; i++)//start from the last element in the smaller shape array,i means total number of elements we get 
                {
                    if (a.Shape[len_a - i] != b.Shape[len_b - i])//if the current i location's elements don't match, broadcast
                    {
                        if (a.Shape[len_a - i] != 1 && b.Shape[len_b - i] != 1)//one of the dimension must be 1
                            throw new RankException("couldn't broadcast in this shape.");
                        if (a.Shape[len_a - i] == 1)
                            shape[shape.Length - i] = b.Shape[len_b - i];
                        if (b.Shape[len_b - i] == 1)
                            shape[shape.Length - i] = a.Shape[len_a - i];
                    }
                }
                // stretch the array to result array's shape.
                a = stretch(a, shape);
                b = stretch(b, shape);
            }
            return Tuple.Create<numpy<T>, numpy<T>>(a, b);
        }

    }
}
