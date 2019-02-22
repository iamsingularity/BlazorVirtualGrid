﻿using BlazorVirtualGridComponent.businessLayer;
using BlazorVirtualGridComponent.classes;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorVirtualGridComponent
{
    public class CompBlazorVirtualGrid_Logic<TItem>: ComponentBase
    {

        [Parameter]
        protected IList<TItem> SourceList { get; set; }

        [Parameter]
        protected string TableName { get; set; }

        [Parameter]
        protected BvgSettings bvgSettings { get; set; } = new BvgSettings();



        public BvgGrid bvgGrid { get; set; }


        protected TItem[] SortedRowsList { get; set; }

        


        public bool ActualRender { get; set; } =false;


        Timer timer1;

        public bool ForceStateHasChaned { get; set; } = false;


        private bool FirstLoad = true;

        private int LastVerticalSkip = -1;

        private int LastHorizontalSkip = -1;



        protected override void OnParametersSet()
        {
       
            bvgGrid = new BvgGrid
            {
                IsReady = true,
                Name = TableName,
                RowsTotalCount = SourceList.Count(),
                bvgSettings = bvgSettings,
                AllProps = typeof(TItem).GetProperties(BindingFlags.Public | BindingFlags.Instance),
                
            };


            bvgGrid.ColumnsOrderedList = ColumnsHelper.GetColumnsOrderedList(bvgGrid);

            SortedRowsList = SourceList.ToArray();
          
            base.OnParametersSet();
        }

        protected override void OnAfterRender()
        {
            if (FirstLoad)
            {
                FirstLoad = false;


                if (bvgGrid.OnSort == null)
                {
                    bvgGrid.OnSort = SortGrid;
                }


                if (bvgGrid.OnVerticalScroll == null)
                {
                    bvgGrid.OnVerticalScroll = OnVerticalScroll;
                }

                if (bvgGrid.OnHorizontalScroll == null)
                {
                    bvgGrid.OnHorizontalScroll = OnHorizontalScroll;
                }

                timer1 = new Timer(Timer1Callback, null, 1, 1);
            }

            base.OnAfterRender();
        }

        public void OnVerticalScroll(int p)
        {
            RenderGridRows(p, false);
        }

        public void OnHorizontalScroll(int p)
        {
            RenderGridColumns(p);
        }

        public void SortGrid(string s)
        {

            SortedRowsList = GenericAdapter<TItem>.GetSortedRowsList(SourceList.AsQueryable(), s).ToArray();

            bvgGrid.CurrVerticalScrollPosition = 0;
            LastVerticalSkip = -1;
            bvgGrid.VericalScroll.compBlazorScrollbar.SetScrollPosition(0);
            
            RenderGridRows(0, false);
        }

        public void RenderGridRows(int skip, bool DotNetOrJsUpdate)
        {

            if (skip != LastVerticalSkip || DotNetOrJsUpdate)
            {

                LastVerticalSkip = skip;


                if (skip > 0)
                {

                    GenericAdapter<TItem>.GetRows(SortedRowsList.Skip(skip).Take(bvgGrid.DisplayedRowsCount),
                        bvgGrid, DotNetOrJsUpdate);
                }
                else
                {
                    GenericAdapter<TItem>.GetRows(SortedRowsList.Take(bvgGrid.DisplayedRowsCount),
                        bvgGrid, DotNetOrJsUpdate);
                }

                if (DotNetOrJsUpdate)
                {
                    bvgGrid.bvgAreaRowsFrozen.InvokePropertyChanged();
                    bvgGrid.bvgAreaRowsNonFrozen.InvokePropertyChanged();
                }
            }
        }

        public int MeasureColumnsCount(int skip)
        {
            int result = 0;

            ColProp[] cols;

            if (skip > 0)
            {
                cols = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen == false).Skip(skip).ToArray();
            }
            else
            {
                cols = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen == false).ToArray();
            }

            result = cols.Count();

           

            int tmp_Width = 0;

            
            double limit = bvgGrid.totalWidth - bvgGrid.FrozenTableWidth;

            
            for (int i = 0; i < cols.Length; i++)
            {
                tmp_Width += cols[i].ColWidth;
                
                if (tmp_Width>limit)
                {
                    
                    return i+1;
                }
            }

            


            return result;
        }


        public int GetSkipedColumns(int scrollPosition)
        {
            int result = 0;

            ColProp[] cols = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen == false).ToArray();


            int tmp_Width = 0;


            for (int i = 0; i < cols.Length; i++)
            {
                tmp_Width += cols[i].ColWidth;

                if (scrollPosition<=tmp_Width)
                {
                    return i;
                }
            }

            return result;
        }

        public void RenderGridColumns(int Scrollposition)
        {
            int skip = Scrollposition==0 ? 0 : GetSkipedColumns(Scrollposition);

            if (skip != LastHorizontalSkip)
            {

                LastHorizontalSkip = skip;

                bvgGrid.DisplayedColumnsCount = MeasureColumnsCount(skip);

                IEnumerable<ColProp> ActiveRegularProps;
                if (skip > 0)
                {
                    ActiveRegularProps = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen == false).Skip(skip).Take(bvgGrid.DisplayedColumnsCount);
                }
                else
                {
                    ActiveRegularProps = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen == false).Take(bvgGrid.DisplayedColumnsCount);
                }



                IEnumerable<ColProp> FrozenProps = bvgGrid.ColumnsOrderedList.Where(x => x.IsFrozen);

                bvgGrid.ActiveProps = new PropertyInfo[FrozenProps.Count() + ActiveRegularProps.Count()];

                int j = 0;
                foreach (var item in FrozenProps)
                {
                    bvgGrid.ActiveProps[j] = item.prop;
                    j++;
                }
                foreach (var item in ActiveRegularProps)
                {
                    bvgGrid.ActiveProps[j] = item.prop;
                    j++;
                }


                List<ColProp> ListProps = FrozenProps.ToList();
                ListProps.AddRange(ActiveRegularProps);


                GenericAdapter<TItem>.GetColumns(ListProps.ToArray(), bvgGrid);

           
                RenderGridRows(LastVerticalSkip, true);

                bvgGrid.bvgAreaColumnsFrozen.InvokePropertyChanged();
                bvgGrid.bvgAreaColumnsNonFrozen.InvokePropertyChanged();
            }


        }

        public void Timer1Callback(object o)
        {
            GetActualWidthAndHeight();
            timer1.Dispose();
        }

       

        public async void GetActualWidthAndHeight()
        {

            bvgGrid.totalWidth = await BvgJsInterop.GetElementActualWidth(bvgGrid.GridTableElementID)-20;


            double top = await BvgJsInterop.GetElementActualTop(bvgGrid.GridTableElementID);
            double windowHeight = await BvgJsInterop.GetWindowHeight();

            bvgGrid.height = windowHeight - top - 40;


            bvgGrid.AdjustSize();

            
            RenderGridColumns(0);
           
            ActualRender = true;
            StateHasChanged();

        }


        public void Refresh()
        {

            bvgGrid.InvokePropertyChanged();

            StateHasChanged();
        }



       
    }
}
