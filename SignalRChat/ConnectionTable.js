    $(document).ready(function () {
 
        //Prepare jtable plugin
        $('#TestTableContainer').jtable({
            title: 'The Student List',
            paging: false, //Enables paging
            // pageSize: 10, //Actually this is not needed since default value is 10.
            sorting: false, //Enables sorting
            //defaultSorting: 'Name ASC', //Optional. Default sorting on first load.
/*
            actions: {
                listAction: '/PagingAndSorting.aspx/StudentList',
                createAction: '/PagingAndSorting.aspx/CreateStudent',
                updateAction: '/PagingAndSorting.aspx/UpdateStudent',
                deleteAction: '/PagingAndSorting.aspx/DeleteStudent'
            },
*/
            fields: {
                StudentId: {
                    key: true,
                    create: false,
                    edit: false,
                    list: false
                },
                Name: {
                    title: 'Name',
                    width: '23%'
                }
            }
        });

        // Populate table
        $('#TestTableContainer').jtable('addRecord', {
            record: {
                StudentId: 1,
                Name: 'Me',
            },
            clientOnly: true
        });
        //Load student list from server
        // $('#StudentTableContainer').jtable('load');
    });