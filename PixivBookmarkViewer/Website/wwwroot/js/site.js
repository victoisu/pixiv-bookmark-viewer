// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.addEventListener('DOMContentLoaded', (e) => {

})

function setThumbnail(thumbnail, id, page) {
    thumbnail.getElementsByTagName("img")[0].src = "/api/Work/" + id + "/thumbnail?page=" + page;
    thumbnail.getElementsByTagName("a")[0].href = "/api/Work/" + id + "/image?page=" + page;
    thumbnail.setAttribute("work-id", id);
    thumbnail.setAttribute("page-number", page);
}

function generateImageElement(id, page) {
    var url = `api/Work/${id}/image?page=${page}`
    var img = document.createElement("IMG");
    img.classList.add("work-image");
    img.src = url;
    return img;
}

function browseUrl(search, page, random) {
    return `Browse?search=${encodeURIComponent(search)}&p=${encodeURIComponent(page)}&random=${encodeURIComponent(random)}`
}

function workUrl(id, page) {
    return `api/Work/${id}/image?page=${page}`;
}